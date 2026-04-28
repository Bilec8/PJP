# FILE + fopen + fwrite — kompletní implementace

Nejnáročnější ze tří kandidátů. **5 souborů**, 2 nové instrukce, nový typ, ~25-35 minut práce.

---

## 🎯 Zadání

Přidat **operace se soubory** — deklarace `FILE`, otevření souboru, zápis do souboru.

```
FILE f;
fopen f "vystup.txt";
fwrite f, "abc", 1+2, "konec";
```

Měl by vytvořit soubor `vystup.txt` s obsahem `abc3konec`.

---

## 🧠 Klíčové koncepty

### 1) FILE je nový typ

V jazyce máme `int`, `float`, `bool`, `string` a teď přidáme `FILE`. Proměnná typu FILE drží **handle** (referenci na otevřený soubor), ne hodnotu.

Brief uvádí 4 typy plus `Error`. My přidáme **`File`** jako pátou hodnotu enumu.

### 2) Type tag pro `open`

Wiki konvence říká: pokud instrukce přijímá hodnotu, která **může být různého typu**, přidej tag (`I`, `F`, `B`, `S`).

`open` přijímá **string** (cestu). Takže by mělo být **`open S`** (i když VM technicky tag nepotřebuje, dokumentace specifikace to vyžaduje).

### 3) `fwrite N` jako `print N`

`fwrite` zapisuje **N hodnot** najednou. Konvence `print N` (počet, ne typ) — takže `fwrite N` je analogický.

VM si typy hodnot zjistí runtime z C# objektů na stacku.

### 4) Žádný default value pro FILE

Když napíšeš `int x;`, VM vyrobí `push I 0; save x;`. Pro FILE žádná **smysluplná default hodnota neexistuje** — `null`? Prázdný handle? **Řešení:** v `VisitDeclaration` udělat early return pro FILE — žádné instrukce se nevygenerují, proměnná v paměti vznikne až po `fopen`.

### 5) Cleanup — soubory musí být zavřené

Pokud jen otevřeš soubor a nezavřeš ho, **data se nemusí zapsat na disk** (operační systém je drží v bufferu). VM musí na konci běhu projít všechny otevřené soubory a zavřít je.

Řešení: `_files` list, `try/finally` v `Run()`, `f.Close()` v `finally`.

---

## 📐 Stack-based překlad

### Deklarace `FILE f;`

| Source | Generované instrukce |
|---|---|
| `FILE f;` | (nic — vynecháme init) |

⚠️ Variable se v paměti VM **vytvoří až po `fopen`**. Než se to stane, `load f` by selhal — uživatel ale takhle psát nemá.

### `fopen f "vystup.txt";`

| Source | Generované instrukce |
|---|---|
| `fopen f "cesta";` | `push S "cesta"`<br>`open S`<br>`save f` |

### `fwrite f, expr1, expr2, ...;`

| Source | Generované instrukce |
|---|---|
| `fwrite f, "abc", 1+2;` | `load f`<br>`push S "abc"`<br>`push I 1`<br>`push I 2`<br>`add I`<br>`fwrite 2` |

⚠️ **`fwrite N`** = počet zapisovaných hodnot, **NEpočítá file handle**. Stack před fwrite: `[..., handle, val1, val2, ..., valN]`.

### Konkrétní příklad — celý program

```
FILE f;
fopen f "vystup.txt";
fwrite f, "abc", 1+2;
```

Vygenerované instrukce:
```
push S "vystup.txt"
open S
save f
load f
push S "abc"
push I 1
push I 2
add I
fwrite 2
```

---

## 1) `PLC.g4`

### A) Přidej `'FILE'` do `primitiveType`:

```antlr
primitiveType
    : 'int'
    | 'float'
    | 'bool'
    | 'string'
    | 'String'
    | 'FILE'                       // ← NEW
    ;
```

### B) Přidej **fopenStatement** a **fwriteStatement** do `statement`:

```antlr
statement
    : ...existing...
    | 'fopen' IDENTIFIER expr ';'                             # fopenStatement
    | 'fwrite' IDENTIFIER (',' expr)+ ';'                     # fwriteStatement
    | ...rest...
    ;
```

⚠️ **Pozor na pořadí** — tyhle dvě alternativy začínají identifierem `fopen`/`fwrite`, takže neměli by konfliktit s ostatními. Ale dej je nad `expr ';'` (exprStatement), ať parser zvolí správně.

---

## 2) `Type.cs`

```csharp
public enum Type
{
    Int, Float, Bool, String, File, Error
    //                        ^^^^ NEW
}
```

---

## 3) `TypeCheckingListener.cs`

### A) V `ExitPrimitiveType` přidej mapping pro `FILE`:

```csharp
public override void ExitPrimitiveType([NotNull] PLCParser.PrimitiveTypeContext context)
{
    Types.Put(context, context.GetText() switch
    {
        "int"    => Type.Int,
        "float"  => Type.Float,
        "bool"   => Type.Bool,
        "string" => Type.String,
        "String" => Type.String,
        "FILE"   => Type.File,                    // ← NEW
        _        => Type.Error
    });
}
```

### B) Přidej `ExitFopenStatement` a `ExitFwriteStatement`:

```csharp
public override void ExitFopenStatement([NotNull] PLCParser.FopenStatementContext context)
{
    var t = SymbolTable[context.IDENTIFIER().Symbol];
    if (t != Type.File && t != Type.Error)
        Errors.ReportError(context.IDENTIFIER().Symbol, "fopen requires FILE variable.");
    
    var pathT = Types.Get(context.expr());
    if (pathT != Type.String && pathT != Type.Error)
        Errors.ReportError(context.expr().Start, "fopen path must be string.");
}

public override void ExitFwriteStatement([NotNull] PLCParser.FwriteStatementContext context)
{
    var t = SymbolTable[context.IDENTIFIER().Symbol];
    if (t != Type.File && t != Type.Error)
        Errors.ReportError(context.IDENTIFIER().Symbol, "fwrite requires FILE variable.");
    // hodnoty mohou být libovolného typu - nekontrolujeme
}
```

**Co metody dělají:**
- **`ExitFopenStatement`** — ověří, že proměnná je typu `File` a cesta je `string`.
- **`ExitFwriteStatement`** — ověří, že proměnná je typu `File`. Hodnoty nekontrolujeme.

> Type checking je **volitelný** — profesor řekl, že u FILE ho lze vynechat. Ale je dobré ho mít pro robustnost.

---

## 4) `CodeGeneratorVisitor.cs`

### A) Uprav `VisitDeclaration` — pro FILE žádný init:

```csharp
public override string VisitDeclaration([NotNull] PLCParser.DeclarationContext context)
{
    var type = _types.Get(context.primitiveType());
    if (type == Type.File) return "";    // ← FILE se neinicializuje
    
    var tag  = Tag(type);
    var def  = DefaultValue(type);
    var sb   = new StringBuilder();
    foreach (var id in context.IDENTIFIER())
    {
        sb.AppendLine($"push {tag} {def}");
        sb.AppendLine($"save {id.Symbol.Text}");
    }
    return sb.ToString();
}
```

### B) Přidej `VisitFopenStatement`:

```csharp
public override string VisitFopenStatement([NotNull] PLCParser.FopenStatementContext context)
{
    var sb = new StringBuilder();
    sb.Append(Visit(context.expr()));                            // path na stack
    sb.AppendLine("open S");                                     // pop string, push handle
    sb.AppendLine($"save {context.IDENTIFIER().Symbol.Text}");   // ulož handle
    return sb.ToString();
}
```

### C) Přidej `VisitFwriteStatement`:

```csharp
public override string VisitFwriteStatement([NotNull] PLCParser.FwriteStatementContext context)
{
    var sb = new StringBuilder();
    sb.AppendLine($"load {context.IDENTIFIER().Symbol.Text}");   // file handle na stack
    foreach (var e in context.expr())
        sb.Append(Visit(e));                                     // hodnoty na stack
    sb.AppendLine($"fwrite {context.expr().Length}");            // fwrite N
    return sb.ToString();
}
```

---

## 5) `VirtualMachine.cs`

### A) Přidej field na trackování souborů (pro flush/close):

```csharp
private readonly List<System.IO.StreamWriter> _files = new List<System.IO.StreamWriter>();
```

### B) Obal `Run()` do `try/finally` pro cleanup:

```csharp
public void Run()
{
    try
    {
        int ip = 0;
        while (ip < _code.Count)
        {
            // ... celý existující switch ...
        }
    }
    finally
    {
        foreach (var f in _files) f.Close();
    }
}
```

### C) Přidej dva case-y do switche:

```csharp
case "open":
{
    // ins[1] = "S" (type tag, neaktivně používaný)
    var path = (string)_stack.Pop();
    var w = new System.IO.StreamWriter(path, append: false);
    _files.Add(w);              // pamatuj si pro close
    _stack.Push(w);             // handle na stack
    ip++;
    break;
}

case "fwrite":
{
    int n = int.Parse(ins[1]);
    
    // Pop N hodnot do pole (odzadu, aby pořadí bylo správné)
    var values = new object[n];
    for (int i = n - 1; i >= 0; i--)
        values[i] = _stack.Pop();
    
    // Pop file handle
    var writer = (System.IO.StreamWriter)_stack.Pop();
    
    // Zapiš hodnoty do souboru
    foreach (var v in values)
        writer.Write(FormatValue(v));
    
    ip++;
    break;
}
```

**Co případy dělají:**

- **`open S`** — popne cestu (string), otevře soubor pro zápis (`append: false` = přepíše existující), uloží do `_files` listu (kvůli close), pushne handle na stack.
- **`fwrite N`** — popne N hodnot do pole, pak file handle, zapíše hodnoty do souboru.

---

## 🧪 Test

`test.in`:
```
FILE f;
fopen f "vystup.txt";
fwrite f, "Hello, ", "World!", "\n";
fwrite f, "Number: ", 42;
fwrite f, "Sum: ", 1 + 2;
```

Spuštění:
```powershell
.\run .\test.in --run
```

Pak ověř obsah:
```powershell
cat vystup.txt
```

Očekávaný výstup:
```
Hello, World!\nNumber: 42Sum: 3
```

(Pozor — `\n` v stringu se zobrazí literálně jako 2 znaky `\` a `n`. Pokud chceš opravdový newline, musel bys přidat escape sekvence do gramatiky.)

---

## 🎯 Pořadí prací

1. **`PLC.g4`** — 3 změny: `'FILE'` v primitiveType, `fopenStatement`, `fwriteStatement`
2. **Build** — ověř regeneraci ANTLR (FopenStatementContext, FwriteStatementContext)
3. **`Type.cs`** — přidej `File` do enum
4. **`TypeCheckingListener.cs`** — `"FILE" => Type.File` + 2 metody
5. **`CodeGeneratorVisitor.cs`** — uprav VisitDeclaration + 2 nové metody
6. **`VirtualMachine.cs`** — `_files` field, `try/finally`, 2 case-y
7. **Test** → demo

**Časem ~25-35 min.** Nejtěžší ze tří kandidátů.

---

## ⚠️ Co může pokazit

| Problém | Příčina | Řešení |
|---|---|---|
| Soubor je prázdný po běhu | Chybí `Close()` — data v bufferu | Zkontroluj `try/finally` v Run() |
| `InvalidCastException` u fwrite | Stack v jiném pořadí, než čekáš | Vytiskni instrukce, projdi krok po kroku |
| `FileNotFoundException` | Soubor se otevírá ke čtení místo zápisu | `new StreamWriter(path, append: false)` |
| Instrukce neobsahuje cestu | `push S` před `open` chybí | Zkontroluj `VisitFopenStatement` |
| `FopenStatementContext` neexistuje | ANTLR neregeneroval | Smaž `obj/`, build znovu |
| Soubor se vytváří v jiné složce | Relativní cesta vs aktuální workdir | Použij absolutní cestu, nebo cd do správné složky |

---

## 💡 Co říct u demo

1. *"Přidal jsem nový typ `FILE` do enumu. Proměnná typu FILE drží StreamWriter handle."*
2. *"Dva nové statementy — `fopen IDENTIFIER expr;` a `fwrite IDENTIFIER, expr, expr, ...;`."*
3. *"`fopen` generuje `push S` (cestu) + `open S` (typ tag pro konzistenci) + `save`. Handle se uloží do paměti."*
4. *"`fwrite` je analogický k `print N` — generuje N pushů hodnot a `fwrite N`. VM popne N hodnot + handle, zapíše do souboru."*
5. *"V VM si soubory pamatuju v listu, abych je mohl zavřít na konci běhu — jinak by data zůstala v bufferu."*
6. *"Deklarace `FILE f;` negeneruje žádné instrukce — FILE nemá smysluplnou default hodnotu, takže se inicializuje až `fopen`."*

---

## 🔑 Otázky a odpovědi

**Q: "Proč nemáš default value pro FILE?"**
> Pro int/float/bool/string je default smysluplný (0, 0.0, false, ""). Pro FILE handle by to bylo `null`, což by stejně vedlo k chybě při použití. Lepší je proměnnou neinicializovat — VM ji vytvoří až `save f` po fopenu.

**Q: "Proč `open S` má tag, když VM ho nepoužívá?"**
> Konzistence s instrukční sadou. Wiki spec říká, že instrukce s typovými parametry mají tag (`add I`, `read T`, atd.). Bez tagu by profesor stáhl body za nedodržení konvence.

**Q: "Proč jsi udělal `fwrite N` a ne `fwrite T1 T2 ... TN`?"**
> Inspirace z `print N`. Počet stačí — VM si typy hodnot zjistí z runtime objektů na stacku. Verze s explicitními tagy by byla extrémně dlouhá při více argumentech.

**Q: "Co se stane, když uživatel zapomene zavolat `fopen`?"**
> `load f` selže s NullReferenceException nebo `KeyNotFoundException`, protože `f` není v paměti. Pro produkční jazyk by tam byla kontrola, pro semestrálku stačí runtime crash.

**Q: "Proč potřebuješ `_files` list a try/finally?"**
> StreamWriter buffer-uje zápisy v paměti pro výkon. Pokud program skončí bez Close/Flush, data se ztratí. `try/finally` zaručí, že se zavře vždycky — i při exception.

**Q: "Funguje to s víc soubory najednou?"**
> Ano. Každý fopen vytvoří nový handle, uloží do `_files` a do proměnné. Můžeš mít otevřených více souborů zároveň. Na konci se všechny zavřou.

**Q: "Jak ošetřuješ soubor, který už existuje?"**
> `new StreamWriter(path, append: false)` — `false` = přepíše existující obsah. Pokud chceš append, dáš `true`. Pro semestrálku přepsání stačí, ale o tomhle bych se možná zmínil.

---

## 🧩 Volitelná rozšíření (pokud zbude čas)

### A) `fclose` instrukce

Pokud chceš explicitně zavírat soubory:

```antlr
| 'fclose' IDENTIFIER ';'                                # fcloseStatement
```

```csharp
public override string VisitFcloseStatement(...)
{
    var sb = new StringBuilder();
    sb.AppendLine($"load {context.IDENTIFIER().Symbol.Text}");
    sb.AppendLine("close");
    return sb.ToString();
}
```

```csharp
case "close":
{
    var w = (System.IO.StreamWriter)_stack.Pop();
    w.Close();
    _files.Remove(w);
    ip++;
    break;
}
```

Pro semestrálku **netřeba** — automatický cleanup v `try/finally` stačí.

### B) `fread` (čtení ze souboru)

Komplikovanější — vyžaduje StreamReader místo Writer, type tag pro čtený typ. Pokud zadání chce **jen zápis**, neimplementuj.

---

Hodně štěstí 🍀
