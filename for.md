# For cyklus `for (init; cond; update) statement` — kompletní implementace

Středně složité rozšíření. **3 soubory** (max 4), ~30 řádků kódu, ~10-15 minut práce.

---

## 🎯 Zadání

Přidat **`for` cyklus** ve stylu C/Java/JavaScript.

```
int i;
for (i = 0; i < 5; i = i + 1)
    write i;
```

Měl by vypsat:
```
0
1
2
3
4
```

---

## 🧠 Klíčové koncepty

### `for` je **desugaring** `while`

```
for (init; cond; update) body
```
je sémanticky ekvivalentní:
```
init;
while (cond) {
    body;
    update;
}
```

Ve VM tak **nepotřebujeme novou instrukci** — recyklujeme `label`, `jmp`, `fjmp` z `while`.

### Tři výrazy v hlavičce

| Část | Co to je | Kdy se vyhodnotí |
|---|---|---|
| `init` | expression | jednou na začátku |
| `cond` | expression vracející bool | před každou iterací |
| `update` | expression | po každé iteraci, před další kontrolou |

V naší gramatice budou všechny tři typu **`expr`** (ne statement). Tělo `for` je samozřejmě statement.

### Pozor na `pop` u init a update

Init i update jsou **expressions** v gramatice, ale v běhu nás jejich hodnota **nezajímá** (dělají side effect — přiřazení).

Stejně jako `expr ;` (exprStatement) musí přidat `pop` po vyhodnocení, **i my musíme přidat `pop`** po init a update v generovaném kódu. Jinak se na stacku hromadí harampádí.

---

## 📐 Stack-based překlad

| Source | Generované instrukce |
|---|---|
| `for (init; cond; update) body` | `<init>`<br>`pop`<br>`label START`<br>`<cond>`<br>`fjmp END`<br>`<body>`<br>`<update>`<br>`pop`<br>`jmp START`<br>`label END` |

### Konkrétní příklad — `for (i = 0; i < 5; i = i + 1) write i;`

```
push I 0          ; init: i = 0
save i
load i            ; (assignment vrací hodnotu)
pop               ; ← discard po init
label 0           ; ⤴ start cyklu
load i            ; cond: i < 5
push I 5
lt I
fjmp 1            ; pokud false, ven
load i            ; body: write i
print 1
load i            ; update: i = i + 1
push I 1
add I
save i
load i
pop               ; ← discard po update
jmp 0             ; skok zpět na start
label 1           ; konec
```

---

## 1) `PLC.g4`

Přidej do `statement`:

```antlr
statement
    : ...existing...
    | 'for' '(' expr ';' expr ';' expr ')' statement         # forStatement
    | ...rest...
    ;
```

**Pojmenované labely** (volitelné, ale hezčí v kódu):
```antlr
| 'for' '(' init=expr ';' cond=expr ';' update=expr ')' statement   # forStatement
```

S nimi pak v code můžeš psát `context.init` místo `context.expr(0)`.

---

## 2) `Type.cs`

**Žádná změna.** Žádný nový typ.

---

## 3) `TypeCheckingListener.cs`

```csharp
public override void ExitForStatement([NotNull] PLCParser.ForStatementContext context)
{
    // expr(1) je podmínka
    var cond = Types.Get(context.expr(1));
    if (cond != Type.Bool && cond != Type.Error)
        Errors.ReportError(context.expr(1).Start, "Condition of 'for' must be bool.");
}
```

S pojmenovanými labely:
```csharp
var cond = Types.Get(context.cond);
```

**Co dělá:**
- Ověří, že prostřední výraz (podmínka) je typu `bool`
- Init a update mohou být cokoliv (přiřazení, expression, ...)

---

## 4) `CodeGeneratorVisitor.cs`

```csharp
public override string VisitForStatement([NotNull] PLCParser.ForStatementContext context)
{
    int startL = NewLabel();
    int endL   = NewLabel();

    var sb = new StringBuilder();
    sb.Append(Visit(context.expr(0)));            // 1) init
    sb.AppendLine("pop");                          //    discard hodnotu
    sb.AppendLine($"label {startL}");              // 2) start cyklu
    sb.Append(Visit(context.expr(1)));            // 3) condition
    sb.AppendLine($"fjmp {endL}");                 //    if false → end
    sb.Append(Visit(context.statement()));        // 4) body
    sb.Append(Visit(context.expr(2)));            // 5) update
    sb.AppendLine("pop");                          //    discard hodnotu
    sb.AppendLine($"jmp {startL}");                // 6) skok zpět
    sb.AppendLine($"label {endL}");                // 7) end label
    return sb.ToString();
}
```

S pojmenovanými labely:
```csharp
sb.Append(Visit(context.init));
sb.Append(Visit(context.cond));
sb.Append(Visit(context.update));
```

**Co dělá:**
- Vytvoří 2 unikátní labels (start, end)
- Vygeneruje **init**, pak `pop` (discard)
- Vyhodnotí **cond**, `fjmp` ven pokud false
- Vykoná **body**
- Vykoná **update**, `pop` (discard)
- Skok zpět na start

---

## 5) `VirtualMachine.cs`

**Žádná změna.** Recykluje existující `label`, `jmp`, `fjmp`, `pop`.

---

## 🧪 Test

`test.in`:
```
// Základní for
int i;
for (i = 0; i < 5; i = i + 1)
    write i;

// For s blokovým tělem
int j;
for (j = 0; j < 3; j = j + 1) {
    write "j =", j;
    write "double:", j * 2;
}

// Vnořený for
int x;
int y;
for (x = 0; x < 3; x = x + 1) {
    for (y = 0; y < 2; y = y + 1) {
        write x, ",", y;
    }
}
```

Spuštění:
```powershell
.\run .\test.in --run
```

Očekávaný výstup:
```
0
1
2
3
4
j =0
double:0
j =1
double:2
j =2
double:4
0,0
0,1
1,0
1,1
2,0
2,1
```

---

## 🎯 Pořadí prací

1. **`PLC.g4`** — 1 alternativa do `statement`
2. **Build** — ověř regeneraci ANTLR
3. **`TypeCheckingListener.cs`** — 1 metoda (jen kontrola podmínky)
4. **`CodeGeneratorVisitor.cs`** — 1 metoda (label-based)
5. **Test** → demo

**Časem ~10-15 min.**

---

## ⚠️ Co může pokazit

| Problém | Příčina | Řešení |
|---|---|---|
| Cyklus se nikdy nezastaví | Update neaktualizuje proměnnou v cond | Zkontroluj `pop` po update — hodnota přiřazení se musí vyhodit |
| `for (;;)` neprojde parserem | Naše gramatika vyžaduje všechny 3 expr | Vyřeš později — chceme `expr?` místo `expr`, ale to je krok navíc |
| Stack se rozjede | Chybí `pop` po init/update | Doplň `pop` ve VisitForStatement |
| `ForStatementContext` neexistuje | ANTLR neregeneroval | Smaž `obj/`, build znovu |

---

## 💡 Co říct u demo

1. *"For jsem implementoval jako desugaring `while` — generované instrukce vypadají jako `init; while(cond) { body; update; }`."*
2. *"Tři výrazy v hlavičce — init, cond, update. Cond musí být bool, ostatní jsou expressions s `pop` na konci."*
3. *"Dvě unikátní label čísla — start a end — pro skok zpět a ven."*
4. *"VM jsem nemusel měnit — všechny instrukce už existovaly z `while`."*

---

## 🔑 Otázky a odpovědi

**Q: "Proč jsi zvolil expr, ne statement, pro init/update?"**
> Aby šlo psát `i = 0` místo `i = 0;`. Statement by vyžadoval středník uvnitř závorek, což není idiomatické. Expression je flexibilnější.

**Q: "Proč ten `pop` po init a update?"**
> Init je obvykle přiřazení, které vrací hodnotu (zůstává na stacku po `save+load`). Když ji nezahodíme, hromadí se. Stejně jako u `expr ;` (exprStatement) musí být `pop`.

**Q: "Co kdyby cond bylo třeba string?"**
> Typecheker hlásí *"Condition of 'for' must be bool."* a stopne kompilaci.

**Q: "Jak je to liší od `while`?"**
> `while` má jen jednu část — podmínku. `for` má tři — init (před cyklem), cond (test každé iterace), update (po těle). Generované instrukce jsou ale v podstatě stejné, jen `for` má init+pop nahoře a update+pop před skokem zpět.

**Q: "Proč jsi nepřidal nové instrukce do VM?"**
> Nepotřebovaly se. `for` cyklus je čistě **strukturální vzor**, který se dá vyjádřit přes existující `label`/`jmp`/`fjmp`. To je výhoda generované instrukční sady — můžeš stavět nové kontrolní struktury bez změny VM.

---

## 🧩 Volitelné rozšíření (pokud zbude čas)

### `for (;;)` — nekonečná smyčka

Změň gramatiku:
```antlr
| 'for' '(' expr? ';' expr ';' expr? ')' statement       # forStatement
```

V codegenu pak musíš ošetřit `null` pro init a update:
```csharp
if (context.expr().Length > 0 && context.expr(0) != null) {
    sb.Append(Visit(context.expr(0)));
    sb.AppendLine("pop");
}
```

Pro semestrální projekt pravděpodobně **netřeba**. Zadání obvykle vyžaduje všechny tři části.

---

Hodně štěstí 🍀
