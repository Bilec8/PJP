# Ternary operátor `cond ? a : b` — kompletní implementace

Nejjednodušší ze tří kandidátů. **3 soubory**, ~30 řádků kódu, ~10 minut práce.

---

## 🎯 Zadání

Přidat **ternary operátor** — zkrácený `if`/`else` jako výraz.

```
int x;
x = 5 < 10 ? 100 : 200;     // x = 100
write x;
write 1 < 2 ? "yes" : "no"; // yes
```

---

## 🧠 Klíčové koncepty

### Ternary je jako `if`/`else`, ale je to **expression** (vrací hodnotu)

| | `if`/`else` | Ternary `? :` |
|---|---|---|
| Forma | Statement | Expression |
| Vrací hodnotu | ❌ | ✅ |
| Použití | Samostatně | V přiřazení, výpočtu |
| Jak generovat | Stejně — labels + skoky | Stejně — labels + skoky |

### Asociativita

Ternary je **pravoasociativní** (jako přiřazení): `a ? b : c ? d : e` = `a ? b : (c ? d : e)`. Proto v gramatice **`<assoc=right>`**.

### Priorita

V C má ternary **velmi nízkou** prioritu — nižší než `||`, vyšší než `=`. V naší gramatice ji umístíme **mezi `or` a `assignment`**.

---

## 📐 Stack-based překlad

| Source | Generované instrukce |
|---|---|
| `cond ? a : b` | `<cond>`<br>`fjmp ELSE`<br>`<a>`<br>`jmp END`<br>`label ELSE`<br>`<b>`<br>`label END` |

**Identické s `if`/`else`**, jen výsledek (hodnota a/b) zůstává na stacku místo aby se zahodil.

### Konkrétní příklad — `5 < 10 ? 100 : 200`

```
push I 5
push I 10
lt I            ; výsledek: true
fjmp 0          ; pokud false → label 0 (else)
push I 100      ; then větev
jmp 1           ; přeskoč else
label 0
push I 200      ; else větev
label 1
                ; na stacku zůstává 100 nebo 200
```

---

## 1) `PLC.g4`

Přidej **mezi `or` a `assignment`** v `expr`:

```antlr
expr
    : ...vyšší priority (not, unaryMinus, mulDivMod, addSubConcat, relational, equality, and, or)...
    | <assoc=right> expr '?' expr ':' expr                    # ternary
    | <assoc=right> IDENTIFIER '=' expr                       # assignment
    | ...zbytek (parens, literals, id)...
    ;
```

⚠️ **Důležité:**
- **Mezi `or` a `assignment`** — priority od nejvyšší k nejnižší.
- **`<assoc=right>`** — pravoasociativní pro řetězení.

---

## 2) `Type.cs`

**Žádná změna.** Žádný nový typ.

---

## 3) `TypeCheckingListener.cs`

```csharp
public override void ExitTernary([NotNull] PLCParser.TernaryContext context)
{
    var cond  = Types.Get(context.expr(0));
    var thenT = Types.Get(context.expr(1));
    var elseT = Types.Get(context.expr(2));

    // Error propagation
    if (cond == Type.Error || thenT == Type.Error || elseT == Type.Error) {
        Types.Put(context, Type.Error);
        return;
    }

    // Podmínka musí být bool
    if (cond != Type.Bool) {
        Errors.ReportError(context.expr(0).Start, "Ternary condition must be bool.");
        Types.Put(context, Type.Error);
        return;
    }

    // Větve musí mít kompatibilní typ
    Type result;
    if (thenT == elseT) {
        result = thenT;
    } else if ((thenT == Type.Int && elseT == Type.Float) ||
               (thenT == Type.Float && elseT == Type.Int)) {
        result = Type.Float;   // int → float promotion
    } else {
        Errors.ReportError(context.Start, $"Ternary branches must have compatible types (got {thenT} and {elseT}).");
        result = Type.Error;
    }

    Types.Put(context, result);
}
```

**Co dělá:**
- Ověří, že podmínka je `bool`
- Ověří, že obě větve mají kompatibilní typ (stejný, nebo numeric s promotion)
- Označí výsledný typ celého výrazu

---

## 4) `CodeGeneratorVisitor.cs`

```csharp
public override string VisitTernary([NotNull] PLCParser.TernaryContext context)
{
    int elseL = NewLabel();
    int endL  = NewLabel();
    var thenT = _types.Get(context.expr(1));
    var elseT = _types.Get(context.expr(2));

    var sb = new StringBuilder();
    sb.Append(Visit(context.expr(0)));               // condition
    sb.AppendLine($"fjmp {elseL}");
    sb.Append(Visit(context.expr(1)));               // then branch
    if (thenT == Type.Int && elseT == Type.Float)
        sb.AppendLine("itof");                       // promotion pokud potřeba
    sb.AppendLine($"jmp {endL}");
    sb.AppendLine($"label {elseL}");
    sb.Append(Visit(context.expr(2)));               // else branch
    if (elseT == Type.Int && thenT == Type.Float)
        sb.AppendLine("itof");                       // promotion v else větvi
    sb.AppendLine($"label {endL}");
    return sb.ToString();
}
```

**Co dělá:**
- Vytvoří 2 unikátní labels (`elseL`, `endL`)
- Vygeneruje stejný pattern jako `if`/`else`, ale **ponechá hodnotu na stacku**
- Vloží `itof` v případě smíšených typů (int v jedné větvi, float v druhé)

---

## 5) `VirtualMachine.cs`

**Žádná změna.** Recykluje existující `fjmp`, `jmp`, `label`, `itof`.

---

## 🧪 Test

`test.in`:
```
int max;
int a;
int b;
a = 5;
b = 10;
max = a > b ? a : b;
write "max =", max;

write 1 < 2 ? "yes" : "no";
write 5 == 5 ? "equal" : "diff";

// Řetězené
int x;
x = 1;
write x == 1 ? "one" : x == 2 ? "two" : "other";
```

Spuštění:
```powershell
.\run .\test.in --run
```

Očekávaný výstup:
```
max =10
yes
equal
one
```

---

## 🎯 Pořadí prací

1. **`PLC.g4`** — 1 alternativa do `expr`
2. **Build** — ověř regeneraci ANTLR (`TernaryContext` musí existovat)
3. **`TypeCheckingListener.cs`** — 1 metoda
4. **`CodeGeneratorVisitor.cs`** — 1 metoda
5. **Test** → demo

**Časem ~10 min.** Nejlehčí ze tří kandidátů.

---

## ⚠️ Co může pokazit

| Problém | Příčina | Řešení |
|---|---|---|
| `a ? b : c = 5` parsuje špatně | Asociativita / priorita | Závorky kolem ternary: `(a ? b : c) = 5` — ale v zadání asi nebude potřeba |
| `1 < 2 ? "a" : 5` projde typecheckem | Mixed string/int — měla by být chyba | Doplň větev v ExitTernary o tuhle kontrolu |
| `TernaryContext` neexistuje | ANTLR neregeneroval | Smaž `obj/`, build znovu |

---

## 💡 Co říct u demo

1. *"Ternary jsem implementoval jako expression, takže vrací hodnotu."*
2. *"Generuje stejné instrukce jako `if`/`else` — `fjmp`, `jmp`, dva labels — ale výsledek zůstává na stacku."*
3. *"V typecheku ověřuju bool podmínku a kompatibilitu typů obou větví."*
4. *"Pravoasociativita pro řetězení `a ? b : c ? d : e`."*
5. *"VM nemusel jsem upravovat — všechny instrukce už existovaly."*

---

## 🔑 Otázky a odpovědi

**Q: "Proč `<assoc=right>`?"**
> Pro správné parsování řetězeného ternary `a ? b : c ? d : e` jako `a ? b : (c ? d : e)`. Defaultní levá by udělala `(a ? b : c) ? d : e` (nesmysl).

**Q: "Proč je ternary v expr a ne v statement?"**
> Protože vrací hodnotu — používá se v přiřazení (`x = a ? b : c`), v argumentech (`write a ? b : c`), atd. `if`/`else` je naopak statement, neumí vracet hodnotu.

**Q: "Jak řešíš typovou kompatibilitu větví?"**
> V typecheku ExitTernary kontroluju, že obě větve mají stejný typ, nebo je jedna int a druhá float (promotion). V codegenu pak vložím `itof` u té int větve, aby výsledek byl konzistentně float.

**Q: "Co kdyby v jedné větvi byl string a v druhé int?"**
> Typecheker hlásí chybu *"branches must have compatible types"* a označí jako Error. Generování pak proběhne, ale výsledek bude označený jako Error a propaguje se nahoru.

**Q: "Jak generuješ instrukce?"**
> Dva labels — elseL, endL. Vyhodnoť podmínku, fjmp do else, then větev, jmp na konec, label else, else větev, label end. Stejný pattern jako `if`, ale výsledek (hodnota a/b) zůstává na stacku.

---

Hodně štěstí 🍀
