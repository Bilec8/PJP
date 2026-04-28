# Pole (`array`) — kompletní implementace

Příklad rozšíření kompilátoru o **pole** (`int a[10]`, `a[1] = 10`, `a[1]`).

Tahle příručka pokrývá **3 různé operace**, které pole vyžaduje, a vysvětluje **proč**.

---

## 🎯 Zadání (z whiteboardu)

```
int a[10];          // declaration
a[1] = 10;          // assignment (write)
10 + a[1];          // access (read)
```

S generovanými instrukcemi:
- `createarray` — vytvoří pole
- `arraysave` — zapíše do pole
- `arrayload` — přečte z pole

---

## 🧠 Klíčové koncepty (než začneš)

### 1) Tři operace s polem

V jazyce existují **3 fundamentálně různé** akce s polem. Každá potřebuje vlastní gramatické pravidlo + metodu:

| Source | Operace | Typ | Generuje |
|---|---|---|---|
| `int a[10];` | **Declaration** | Statement | `createarray` |
| `a[1] = 10;` | **Assignment** (write) | Expression | `arraysave` |
| `a[1]` (uvnitř výrazu) | **Access** (read) | Expression | `arrayload` |

**Jak to poznat ze zadání:** podívej se na každý řádek příkladu a zařaď ho do jedné z těchto kategorií. Kolik kategorií najdeš, tolik gramatických pravidel a metod potřebuješ.

### 2) Asociativita — kdy `<assoc=right>`?

**Mentální test:** *"Když napíšu `a OP b OP c`, chci to jako `(a OP b) OP c` nebo `a OP (b OP c)`?"*

| Volba | Použití | Příklady |
|---|---|---|
| **Left** (default) | Většina operátorů | `+`, `-`, `*`, `/`, `<`, `>`, `==`, `&&`, `||`, `.` |
| **Right** (`<assoc=right>`) | Přiřazení a ternary | `=`, `?:`, `^` (mocnina) |

**Pravidlo:** pokud rule obsahuje `=`, dej `<assoc=right>`. Bezpečná pojistka pro řetězené přiřazení (`a[0] = b[0] = 10`).

### 3) Jména metod = labely v gramatice

```antlr
| primitiveType IDENTIFIER '[' INT ']' ';'     # arrayDeclaration
                                                  ▲
                                          tohle jméno
                                                  ▼
        ExitArrayDeclaration  /  VisitArrayDeclaration  /  ArrayDeclarationContext
```

Konvence:
- **Gramatika:** `camelCase`
- **C# metody:** `PascalCase` (ANTLR automaticky kapitalizuje první písmeno)

---

## 📐 Stack-based překlad (mentální model)

| Source | Vygenerované instrukce |
|---|---|
| `int a[10];` | `push I 10`<br>`createarray I`<br>`save a` |
| `a[1] = 10;` (jako exprStatement) | `push I 10`<br>`load a`<br>`push I 1`<br>`arraysave`<br>`pop` |
| `a[1]` (čtení uvnitř výrazu) | `load a`<br>`push I 1`<br>`arrayload` |

### Stack během operací

**arraysave:** pop tří hodnot (idx, array, value), uloží `array[idx] = value`, pushne `value` zpět (assignment vrací hodnotu)

```
Stack před:  [..., value, array, index]
Stack po:    [..., value]
```

**arrayload:** pop dvou hodnot (idx, array), pushne `array[idx]`

```
Stack před:  [..., array, index]
Stack po:    [..., array[index]]
```

---

## 1) `PLC.g4`

### A) Přidej **arrayDeclaration** do `statement`:

```antlr
statement
    : ...existing...
    | primitiveType IDENTIFIER '[' INT ']' ';'                # arrayDeclaration
    | ...rest...
    ;
```

### B) Přidej **arrayAssignment** a **arrayAccess** do `expr`:

```antlr
expr
    : ...high-priority unary, mul, add, etc...
    | <assoc=right> IDENTIFIER '[' expr ']' '=' expr          # arrayAssignment
    | IDENTIFIER '[' expr ']'                                 # arrayAccess
    | ...low-priority assignment, parens, literals...
    ;
```

⚠️ **Důležité:**
1. **`arrayAssignment` PŘED `arrayAccess`** — jinak parser zvolí access a uvidí `=` jako neočekávaný token.
2. **`<assoc=right>`** — pojistka pro řetězené přiřazení.

---

## 2) `Type.cs`

**Žádná změna.** Element typ uložíme do SymbolTable jako prostý `Type.Int`/`Type.Float`/atd. Není to úplně přesné (proměnná `a` je array, ne int), ale stačí to. VM rozliší podle skutečné runtime hodnoty (`object[]` vs `int`).

> Alternativně bys mohl přidat `Type.Array` do enumu, ale pak by si musel hlídat element type zvlášť — větší zásah, jen kosmetický rozdíl.

---

## 3) `TypeCheckingListener.cs`

```csharp
public override void ExitArrayDeclaration([NotNull] PLCParser.ArrayDeclarationContext context)
{
    var type = Types.Get(context.primitiveType());
    SymbolTable.Add(context.IDENTIFIER().Symbol, type);   // uložíme element type
}

public override void ExitArrayAccess([NotNull] PLCParser.ArrayAccessContext context)
{
    var elemType = SymbolTable[context.IDENTIFIER().Symbol];
    var idxType  = Types.Get(context.expr());
    if (idxType != Type.Int && idxType != Type.Error)
        Errors.ReportError(context.expr().Start, "Array index must be int.");
    Types.Put(context, elemType);
}

public override void ExitArrayAssignment([NotNull] PLCParser.ArrayAssignmentContext context)
{
    var elemType = SymbolTable[context.IDENTIFIER().Symbol];
    var idxType  = Types.Get(context.expr(0));
    if (idxType != Type.Int && idxType != Type.Error)
        Errors.ReportError(context.expr(0).Start, "Array index must be int.");
    Types.Put(context, elemType);   // assignment vrací hodnotu typu prvku
}
```

**Co každá metoda dělá:**

- **`ExitArrayDeclaration`** — registruje proměnnou v SymbolTable s typem prvku (`int a[10]` uloží `a → Int`)
- **`ExitArrayAccess`** — ověří, že index je int. Označí typ výrazu `a[1]` na typ prvku.
- **`ExitArrayAssignment`** — ověří index. Označí výsledný typ na typ prvku (assignment vrací hodnotu).

---

## 4) `CodeGeneratorVisitor.cs`

```csharp
public override string VisitArrayDeclaration([NotNull] PLCParser.ArrayDeclarationContext context)
{
    var type = _types.Get(context.primitiveType());
    var size = context.INT().Symbol.Text;
    var sb = new StringBuilder();
    sb.AppendLine($"push I {size}");
    sb.AppendLine($"createarray {Tag(type)}");
    sb.AppendLine($"save {context.IDENTIFIER().Symbol.Text}");
    return sb.ToString();
}

public override string VisitArrayAccess([NotNull] PLCParser.ArrayAccessContext context)
{
    var sb = new StringBuilder();
    sb.AppendLine($"load {context.IDENTIFIER().Symbol.Text}");   // pole na stack
    sb.Append(Visit(context.expr()));                            // index na stack
    sb.AppendLine("arrayload");
    return sb.ToString();
}

public override string VisitArrayAssignment([NotNull] PLCParser.ArrayAssignmentContext context)
{
    var sb = new StringBuilder();
    sb.Append(Visit(context.expr(1)));                           // value (RHS)
    sb.AppendLine($"load {context.IDENTIFIER().Symbol.Text}");   // pole
    sb.Append(Visit(context.expr(0)));                           // index
    sb.AppendLine("arraysave");
    return sb.ToString();
}
```

**Co každá metoda dělá:**

- **`VisitArrayDeclaration`** — vygeneruje `push N` + `createarray T` + `save a`. `N` je velikost (literál v gramatice), `T` je tag prvku.
- **`VisitArrayAccess`** — vygeneruje `load a` + index + `arrayload`. Pole se naloaduje, index vyhodnotí, instrukce sebere oba a pushne hodnotu.
- **`VisitArrayAssignment`** — vygeneruje hodnotu + `load a` + index + `arraysave`. **Pořadí na stacku** je klíčové — VM popne v tomto pořadí: idx, array, value.

---

## 5) `VirtualMachine.cs`

Přidej tři case-y do switche v `Run()`:

```csharp
case "createarray":
{
    int size = (int)_stack.Pop();
    object def = ins[1] switch
    {
        "I" => (object)0,
        "F" => 0.0f,
        "B" => false,
        "S" => "",
        _   => 0
    };
    var arr = new object[size];
    for (int i = 0; i < size; i++) arr[i] = def;
    _stack.Push(arr);
    ip++;
    break;
}

case "arraysave":
{
    int idx = (int)_stack.Pop();
    var arr = (object[])_stack.Pop();
    var val = _stack.Pop();
    arr[idx] = val;
    _stack.Push(val);   // assignment vrací hodnotu
    ip++;
    break;
}

case "arrayload":
{
    int idx = (int)_stack.Pop();
    var arr = (object[])_stack.Pop();
    _stack.Push(arr[idx]);
    ip++;
    break;
}
```

**Co každý case dělá:**

- **`createarray T`** — popne velikost, vytvoří `object[size]` inicializované na default hodnotu typu `T`, pushne pole.
- **`arraysave`** — popne 3 hodnoty (idx, array, value). Zapíše `array[idx] = value`. Pushne value zpět (kvůli `assignment returns value`).
- **`arrayload`** — popne 2 hodnoty (idx, array). Pushne `array[idx]`.

---

## 🧪 Test

`test.in`:
```
int a[10];
a[1] = 10;
a[2] = 20;
write a[1];
write a[2];
write a[1] + a[2];
```

Spuštění:
```powershell
.\run .\test.in --run
```

Očekávaný výstup:
```
10
20
30
```

---

## 🎯 Pořadí prací (na cviku)

1. **`PLC.g4`** — 3 nová pravidla
2. **Build** — ověř, že ANTLR pochopil novou gramatiku (smaž `obj/` pokud `*Context` třídy nejsou rozpoznané)
3. **`TypeCheckingListener.cs`** — 3 nové `Exit*` metody
4. **`CodeGeneratorVisitor.cs`** — 3 nové `Visit*` metody
5. **`VirtualMachine.cs`** — 3 nové case-y
6. **Build** → vytvoř `test.in` → spusť → ukaž

**Časem ~20-25 min**, pokud nezapomeneš pořadí v gramatice (arrayAssignment PŘED arrayAccess) a `<assoc=right>`.

---

## ⚠️ Co může pokazit

| Problém | Příčina | Řešení |
|---|---|---|
| Parser hlásí "no viable alternative" u `a[1] = 10` | `arrayAccess` je v gramatice před `arrayAssignment` | Prohoď pořadí |
| `IndexOutOfRangeException` v VM | Index mimo pole | Pro cvičení nech tak (bounds check je extra) |
| `InvalidCastException` při `arraysave` | Stack v jiném pořadí, než čekáš | Vytiskni instrukce, projdi krok po kroku |
| `*Context` třídy neexistují | ANTLR neregeneroval | Smaž `PLC_Project/PLC_Project/obj/`, build znovu |
| Cizí typy v poli | Default value pro `String[]` byl `null` | Zkontroluj `createarray` v VM — defaultní hodnoty pro každý typ |

---

## 🧩 Co se neimplementuje (a stačí to)

- ❌ **Bounds check** — `a[100]` na poli velikosti 10 spadne. OK pro cvičení.
- ❌ **Multi-dimenzionální pole** — `int a[3][4]`. Není v zadání.
- ❌ **Dynamická velikost** — `int a[n]` kde `n` je proměnná. Náš parser bere jen `INT` literál.
- ❌ **Element type tracking** — `int[]` a `float[]` se v typecheku neliší. OK.

---

## 💡 Co říct u demo

1. *"Pole jsem implementoval jako 3 různé operace — declaration, write access (assignment), read access. Každá generuje jinou instrukci."*
2. *"V VM používám `object[]` jako reprezentaci. Pole se na stacku předává jako reference."*
3. *"Createarray inicializuje na default podle tagu typu — 0 pro int, 0.0 pro float, atd."*
4. *"Index ve `arraysave`/`arrayload` je dynamický, vyhodnotí se jako expr, takže `a[i+1]` taky funguje."*

---

## 🔑 Otázky a odpovědi (kdyby se zeptal)

**Q: "Proč máš `<assoc=right>` u arrayAssignment?"**
> Aby `a[0] = b[0] = 10` parsovalo jako `a[0] = (b[0] = 10)`. Defaultní levá asociativita by udělala `(a[0] = b[0]) = 10`, což je nesmysl.

**Q: "Proč 3 různá pravidla? Nešlo by to jedním?"**
> Mohly by být kombinované, ale každá operace generuje jiné instrukce. Tři pravidla = tři čisté metody, žádné rozhodování v kódu.

**Q: "Co se stane když `a[100]` na poli velikosti 10?"**
> VM hodí IndexOutOfRangeException. Pro produkční jazyk by tam byl bounds check, pro cvičení stačí.

**Q: "Jak je array v paměti VM?"**
> Jako `object[]` v `_memory` slovníku. `load a` ho pushne na stack, `arrayload`/`arraysave` s ním pracují přes index.

**Q: "Můžeš použít proměnnou jako index?"**
> Ano, `a[i+1]` funguje. Index v gramatice je `expr`, vyhodnocuje se na stack a pak `arrayload` ho popne.

---

Hodně štěstí 🍀
