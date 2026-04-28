# Templates pro rozšíření projektu

Tahle příručka je **pro live cvičení**, kdy dostaneš úkol a potřebuješ ho rychle zaimplementovat. Najdi typ rozšíření, zkopíruj šablonu, doplň specifika.

---

## 📋 Postup pro jakékoli rozšíření

Vždycky stejných **5 míst** v projektu:

| # | Soubor | Co tam přidat | Vždy? |
|---|---|---|---|
| 1 | `PLC.g4` | Nové gramatické pravidlo / alternativa | ✅ |
| 2 | `Type.cs` | Nová hodnota v enum | jen pokud nový typ |
| 3 | `TypeCheckingListener.cs` | `Exit*` metoda | volitelné |
| 4 | `CodeGeneratorVisitor.cs` | `Visit*` metoda | ✅ |
| 5 | `VirtualMachine.cs` | `case` ve switche | jen pokud nová instrukce |

**Pořadí práce:** vždycky **shora dolů** (1 → 5).

---

## 🎯 Identifikace typu rozšíření

Než začneš kódit, **zařaď úkol** do jedné z kategorií:

| Typ úkolu | Příklady |
|---|---|
| **Nová binární expression** | `charAt`, string concat, bit-shift |
| **Nová unární expression** | string length, abs |
| **Nový literál** | hex int, raw string |
| **Nový statement** | `print` varianta, file ops |
| **Nový typ** | `char`, `FILE`, array |
| **Nová control flow konstrukce** | `for`, `do-while`, `switch`, ternary |
| **Indexování / volání** | `s[i]`, `f(x)` |

Každá kategorie má vlastní šablonu níže.

---

## 🟦 Šablona 1: Nová binární expression

**Use case:** přidat operátor jako `**` (mocnina), `<<` (bit shift), nebo string operaci.

### `PLC.g4`

Přidej do `expr` jako alternativu (pozor na **prioritu** — vyšší priorita = výš v pravidle):

```antlr
| expr 'TVOJ_OP' expr                                     # mojeOp
```

Pokud chceš pravou asociativitu (jako `=`):
```antlr
| <assoc=right> expr 'TVOJ_OP' expr                       # mojeOp
```

### `TypeCheckingListener.cs`

```csharp
public override void ExitMojeOp([NotNull] PLCParser.MojeOpContext context)
{
    var left  = Types.Get(context.expr(0));
    var right = Types.Get(context.expr(1));
    
    // Error propagation
    if (left == Type.Error || right == Type.Error) {
        Types.Put(context, Type.Error);
        return;
    }
    
    // Validace typů (uprav podle úkolu)
    if (left != Type.Int || right != Type.Int) {
        Errors.ReportError(context.Start, "Operator 'TVOJ_OP' requires int operands.");
        Types.Put(context, Type.Error);
        return;
    }
    
    Types.Put(context, Type.Int);  // typ výsledku
}
```

### `CodeGeneratorVisitor.cs`

```csharp
public override string VisitMojeOp([NotNull] PLCParser.MojeOpContext context)
{
    return Visit(context.expr(0)) +    // levá strana
           Visit(context.expr(1)) +    // pravá strana
           "mojeInstrukce\n";          // operace
}
```

### `VirtualMachine.cs`

```csharp
case "mojeInstrukce":
{
    var right = _stack.Pop();
    var left  = _stack.Pop();
    _stack.Push(/* tvoje operace s left a right */);
    ip++;
    break;
}
```

### Příklad: `charAt` (string indexing)

```antlr
// PLC.g4 — jako úplně první alternativa expr (nejvyšší priorita)
| expr '[' expr ']'                                       # indexing
```

```csharp
// CodeGen
public override string VisitIndexing([NotNull] PLCParser.IndexingContext context)
{
    return Visit(context.expr(0)) +
           Visit(context.expr(1)) +
           "charAt\n";
}

// VM
case "charAt":
{
    var idx = (int)_stack.Pop();
    var str = (string)_stack.Pop();
    _stack.Push(str[idx].ToString());
    ip++;
    break;
}
```

---

## 🟪 Šablona 2: Nová unární expression

**Use case:** prefix nebo postfix operátor — `length`, `abs`, `++`.

### `PLC.g4`

Přidej do `expr` na pozici podle priority (unární typicky vysoko):

```antlr
| 'TVUJ_OP' expr                                          # mojeUnary
```

### `TypeCheckingListener.cs`

```csharp
public override void ExitMojeUnary([NotNull] PLCParser.MojeUnaryContext context)
{
    var t = Types.Get(context.expr());
    if (t == Type.Error) { Types.Put(context, Type.Error); return; }
    
    // Validace
    if (t != Type.String) {  // uprav podle úkolu
        Errors.ReportError(context.expr().Start, "'TVUJ_OP' requires string.");
        Types.Put(context, Type.Error);
        return;
    }
    
    Types.Put(context, Type.Int);  // typ výsledku
}
```

### `CodeGeneratorVisitor.cs`

```csharp
public override string VisitMojeUnary([NotNull] PLCParser.MojeUnaryContext context)
{
    return Visit(context.expr()) + "mojeInstrukce\n";
}
```

### `VirtualMachine.cs`

```csharp
case "mojeInstrukce":
{
    var v = _stack.Pop();
    _stack.Push(/* operace s v */);
    ip++;
    break;
}
```

### Příklad: `length s` (délka stringu)

```antlr
| 'length' expr                                           # length
```

```csharp
// CodeGen
public override string VisitLength([NotNull] PLCParser.LengthContext context)
{
    return Visit(context.expr()) + "strlen\n";
}

// VM
case "strlen":
{
    var s = (string)_stack.Pop();
    _stack.Push(s.Length);
    ip++;
    break;
}
```

---

## 🟩 Šablona 3: Nový statement

**Use case:** nový druh příkazu jako `fopen`, `assert`, `print_line`.

### `PLC.g4`

Přidej do `statement` jako alternativu:

```antlr
| 'KEYWORD' arg1 arg2 ... ';'                             # mujStatement
```

Příklady struktury:
```antlr
| 'fopen' IDENTIFIER expr ';'                             # fopenStatement
| 'assert' expr ';'                                       # assertStatement
| 'print' '(' expr (',' expr)* ')' ';'                    # printStatement
```

### `TypeCheckingListener.cs`

```csharp
public override void ExitMujStatement([NotNull] PLCParser.MujStatementContext context)
{
    // Validace argumentů (volitelné)
    var t = Types.Get(context.expr());
    if (t != Type.String && t != Type.Error) {
        Errors.ReportError(context.expr().Start, "Argument must be string.");
    }
}
```

### `CodeGeneratorVisitor.cs`

```csharp
public override string VisitMujStatement([NotNull] PLCParser.MujStatementContext context)
{
    var sb = new StringBuilder();
    sb.Append(Visit(context.expr()));         // vyhodnoť argument
    sb.AppendLine("mojeInstrukce");
    return sb.ToString();
}
```

### `VirtualMachine.cs`

```csharp
case "mojeInstrukce":
{
    var arg = _stack.Pop();
    /* udělej něco */
    ip++;
    break;
}
```

### Příklad: `fopen f "soubor.txt";`

```antlr
| 'fopen' IDENTIFIER expr ';'                             # fopenStatement
```

```csharp
// CodeGen
public override string VisitFopenStatement([NotNull] PLCParser.FopenStatementContext context)
{
    var sb = new StringBuilder();
    sb.Append(Visit(context.expr()));
    sb.AppendLine("open");
    sb.AppendLine($"save {context.IDENTIFIER().Symbol.Text}");
    return sb.ToString();
}

// VM
case "open":
{
    var path = (string)_stack.Pop();
    var w = new System.IO.StreamWriter(path, append: false);
    _files.Add(w);  // pamatuj si pro close
    _stack.Push(w);
    ip++;
    break;
}
```

---

## 🟨 Šablona 4: Statement s variabilním počtem argumentů

**Use case:** `print x, y, z`, `fwrite f, val1, val2`, `read a, b, c`.

### `PLC.g4`

```antlr
| 'KEYWORD' IDENTIFIER (',' expr)+ ';'                    # mujMultiStatement
```

### `CodeGeneratorVisitor.cs`

```csharp
public override string VisitMujMultiStatement([NotNull] PLCParser.MujMultiStatementContext context)
{
    var sb = new StringBuilder();
    sb.AppendLine($"load {context.IDENTIFIER().Symbol.Text}");  // např. file handle
    foreach (var e in context.expr())
        sb.Append(Visit(e));                                    // všechny hodnoty na stack
    sb.AppendLine($"mojeInstrukce {context.expr().Length}");    // počet
    return sb.ToString();
}
```

### `VirtualMachine.cs`

```csharp
case "mojeInstrukce":
{
    int n = int.Parse(ins[1]);
    
    // Pop N hodnot do pole (odzadu, aby pořadí bylo správné)
    var values = new object[n];
    for (int i = n - 1; i >= 0; i--)
        values[i] = _stack.Pop();
    
    var handle = _stack.Pop();  // file/první argument
    
    // Použij hodnoty ve správném pořadí
    foreach (var v in values) {
        /* udělej něco s handle a v */
    }
    
    ip++;
    break;
}
```

---

## 🟧 Šablona 5: Nový control flow (if-like, while-like, for)

**Use case:** `for`, `do-while`, `switch`, `ternary`, `repeat-until`.

### `PLC.g4`

```antlr
| 'KEYWORD' '(' init=expr ';' cond=expr ';' update=expr ')' statement   # forStatement
```

(Pojmenování přes `init=`, `cond=`, `update=` ti umožní v code je adresovat hezčí.)

### `CodeGeneratorVisitor.cs`

#### Pattern: while/for (loop dopředu)

```csharp
public override string VisitForStatement([NotNull] PLCParser.ForStatementContext context)
{
    int startL = NewLabel();
    int endL   = NewLabel();
    
    var sb = new StringBuilder();
    
    sb.Append(Visit(context.init));        // 1) init
    sb.AppendLine("pop");                  //    expr-statement → discard
    
    sb.AppendLine($"label {startL}");      // 2) label loop start
    sb.Append(Visit(context.cond));        // 3) condition
    sb.AppendLine($"fjmp {endL}");         //    if false → end
    
    sb.Append(Visit(context.statement())); // 4) body
    sb.Append(Visit(context.update));      // 5) update
    sb.AppendLine("pop");
    
    sb.AppendLine($"jmp {startL}");        // 6) jump back
    sb.AppendLine($"label {endL}");        // 7) end label
    
    return sb.ToString();
}
```

#### Pattern: ternary `cond ? a : b`

```csharp
public override string VisitTernary([NotNull] PLCParser.TernaryContext context)
{
    int elseL = NewLabel();
    int endL  = NewLabel();
    
    var sb = new StringBuilder();
    sb.Append(Visit(context.expr(0)));      // condition
    sb.AppendLine($"fjmp {elseL}");         // false → else
    sb.Append(Visit(context.expr(1)));      // then branch
    sb.AppendLine($"jmp {endL}");           // skip else
    sb.AppendLine($"label {elseL}");
    sb.Append(Visit(context.expr(2)));      // else branch
    sb.AppendLine($"label {endL}");
    return sb.ToString();
}
```

#### Pattern: do-while (loop s testem na konci)

```csharp
public override string VisitDoWhile([NotNull] PLCParser.DoWhileContext context)
{
    int startL = NewLabel();
    
    var sb = new StringBuilder();
    sb.AppendLine($"label {startL}");
    sb.Append(Visit(context.statement()));   // tělo
    sb.Append(Visit(context.expr()));        // podmínka
    sb.AppendLine("not");                    // negace — skoč zpět pokud TRUE
    sb.AppendLine($"fjmp {startL}");         // (fjmp = jump if false po negaci)
    return sb.ToString();
}
```

### `VirtualMachine.cs`

**Žádná změna** — existující `jmp`, `fjmp`, `label` stačí.

---

## 🟥 Šablona 6: Nový typ (`char`, `FILE`, array)

### `Type.cs`

```csharp
public enum Type { Int, Float, Bool, String, Char, Error }
//                                          ^^^^ NEW
```

⚠️ **Tip pro úsporu času:** pokud nový typ je "podobný stringu" (jako `char`), můžeš ho v `ExitPrimitiveType` namapovat na `Type.String` — pak nemusíš měnit nic dalšího v typecheckeru a VM:

```csharp
"char" => Type.String,    // alias na string
```

### `PLC.g4`

```antlr
primitiveType
    : 'int' | 'float' | 'bool' | 'string' | 'String' | 'NOVY_TYP'
    ;
```

### `TypeCheckingListener.cs` — v `ExitPrimitiveType`:

```csharp
"NOVY_TYP" => Type.NovyTyp,
```

### `CodeGeneratorVisitor.cs` — uprav `Tag` a `DefaultValue`:

```csharp
private static string Tag(Type t) => t switch
{
    Type.Int    => "I",
    Type.Float  => "F",
    Type.Bool   => "B",
    Type.String => "S",
    Type.NovyTyp => "X",   // ← NEW
    _           => "I"
};

private static string DefaultValue(Type t) => t switch
{
    Type.Int    => "0",
    Type.Float  => "0.0",
    Type.Bool   => "false",
    Type.String => "\"\"",
    Type.NovyTyp => "/* default */",   // ← NEW
    _           => "0"
};
```

⚠️ **Pro typy bez smysluplného defaultu** (jako `FILE`) — udělej v `VisitDeclaration` early-return:

```csharp
if (type == Type.File) return "";   // FILE se neinicializuje
```

### `VirtualMachine.cs` — uprav `ExecPush`:

```csharp
case "X": _stack.Push(/* parsing pro nový typ */); break;
```

A `FormatValue`, pokud chceš jiný formát při tisku:

```csharp
private static string FormatValue(object v) => v switch
{
    float f => f.ToString(CultureInfo.InvariantCulture),
    bool b  => b ? "true" : "false",
    NovyTyp x => /* tvůj formát */,
    _       => v.ToString() ?? ""
};
```

---

## 🛠️ Společné patterny v VM

### Pop hodnot do pole (zachová pořadí)

```csharp
int n = int.Parse(ins[1]);
var values = new object[n];
for (int i = n - 1; i >= 0; i--)
    values[i] = _stack.Pop();
```

### Cast hodnoty ze stacku

```csharp
var i = (int)_stack.Pop();         // pokud víš, že je int
var f = (float)_stack.Pop();
var s = (string)_stack.Pop();
var b = (bool)_stack.Pop();
```

⚠️ **Pokud nevíš jistě typ**, použij `is`:
```csharp
var v = _stack.Pop();
if (v is int i)        { /* ... */ }
else if (v is float f) { /* ... */ }
```

### Skoky a labels

```csharp
case "jmp":   ip = _labels[int.Parse(ins[1])];                                     break;
case "fjmp":  ip = (bool)_stack.Pop() ? ip + 1 : _labels[int.Parse(ins[1])];        break;
case "label": ip++;                                                                 break;
```

`_labels` je předem připravená mapa `label číslo → řádka`. **Nepřepisuj!**

### Always increment ip

Každý case **musí** buď:
- Udělat `ip++` (jdi na další)
- Nebo nastavit `ip = něco` (skok)

Bez toho se VM zacyklí.

---

## 🧪 Testovací šablona

Vytvoř `test.in` v rootu projektu:

```
// Tady tvůj testovací kód s novou featurou
write "ahoj";
```

Spusť:

```powershell
.\run .\test.in              # zkontroluj instrukce
.\run .\test.in --run        # zkontroluj výstup
```

---

## 🎬 Demo posloupnost (na cviku)

```powershell
# 1. Ukaž source code
cat .\test.in

# 2. Ukaž vygenerované instrukce
.\run .\test.in

# 3. Spusť
.\run .\test.in --run
```

3 příkazy. Nic víc.

---

## 🚨 Časté problémy a řešení

| Problém | Příčina | Řešení |
|---|---|---|
| `Visit*Context` neexistuje | ANTLR neregeneroval | Smaž `obj/`, build znovu |
| `InvalidCastException` ve VM | Špatný typ na stacku | Vytiskni instrukce, projdi ručně |
| VM se zacyklí | Chybí `ip++` v case | Přidej `ip++` před `break` |
| Build error v gramatice | Levá rekurze nebo ambiguita | Přesuň alternativy v `expr` |
| Stack se rozjede | Chybí `pop` u expr-statement | Zkontroluj `VisitExprStatement` logiku |
| Floaty se tisknou bez `.0` | `float.ToString()` default | Cosmetické, nech být |

---

## ⚡ Rychlá rekapitulace pipeline (pro vysvětlení)

```
zdroják (test.in)
    ↓
LEXER         (PLC.g4 → tokeny)
    ↓
PARSER        (PLC.g4 → parse tree)
    ↓
TYPECHECK     (Listener → ověří typy, hlásí errory)
    ↓
CODEGEN       (Visitor → vygeneruje instrukce)
    ↓
string instrukcí
    ↓
VIRTUAL MACHINE  (interpret na zásobníku)
    ↓
výstup programu
```

---

## 💡 Klíčové fráze pro vysvětlení

| Co říct | Místo čeho |
|---|---|
| "Parse tree" | "ten strom" |
| "Post-order traversal" | "obejdu strom" |
| "Listener pattern" | "ta třída co reaguje na uzly" |
| "Visitor pattern" | "ta druhá třída" |
| "Stack-based architektura" | "ten zásobník" |
| "Label-based control flow" | "skoky" |
| "Type promotion" | "převod typu" |
| "Operátorová priorita přes pořadí alternativ" | "priorita" |
| "Pravá asociativita" | "zprava doleva" |

---

## 🎯 Workflow během cvika

1. **Pochop zadání** (2 min) — sepiš si syntax, instrukce, sémantiku
2. **Identifikuj typ rozšíření** (1 min) — najdi šablonu v tomto dokumentu
3. **Implementuj** (15-20 min) — kopíruj šablonu, doplňuj specifika
4. **Build** (1 min) — `dotnet build`, oprav errory
5. **Test** (3 min) — vytvoř `test.in`, spusť, ověř
6. **Vysvětli** (5-10 min) — projdi 4 soubory, vysvětli změny, ukaž demo

**Celkem ~30 minut.** Pokud běžíš přes čas, řekni profesorovi co stíháš a co zbývá.

---

Hodně štěstí 🍀
