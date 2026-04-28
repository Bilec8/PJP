# Spuštění projektu — cheat sheet

Vždy spouštěj **z rootu projektu** (složka `PJP/`, kde je `run.bat` a `run.sh`).

```powershell
cd C:\Users\simon\Projects\school\PJP
```

---

## Windows PowerShell

```powershell
# Vypsat instrukce na obrazovku (bez spuštění VM)
.\run .\samples\PLC_t1.in

# Zkompilovat a rovnou spustit ve VM
.\run .\samples\PLC_t1.in --run

# Vlastní testovací soubor
.\run .\test.in
.\run .\test.in --run
```

---

## Windows CMD

```cmd
run samples\PLC_t1.in
run samples\PLC_t1.in --run
```

---

## Linux / Mac

```bash
chmod +x run.sh                    # jednou, aby šel skript spustit
./run.sh samples/PLC_t1.in
./run.sh samples/PLC_t1.in --run
```

---

## Co dělá `--run`

| Bez `--run` | Vypíše vygenerované instrukce na obrazovku |
| S `--run` | Zkompiluje a rovnou vykoná instrukce ve VM (program poběží) |

---

## Testovací soubory v projektu

| Soubor | Co testuje |
|---|---|
| `samples/PLC_t1.in` | Konstanty, proměnné, výrazy, multiple assignment, **read** (vyžaduje 4 vstupy) |
| `samples/PLC_t2.in` | Relační a logické operátory |
| `samples/PLC_t3.in` | Control flow — `if`/`else`, `while`, **read** (1 vstup) |
| `samples/PLC_errors.in` | Chyby — typecheker by měl nahlásit 6 errorů |

### Co dát na vstup u `PLC_t1.in --run`

Program čeká 4 řádky (int, float, string, bool):
```
42
3.14
ahoj
true
```

### Co dát na vstup u `PLC_t3.in --run`

Program čeká 1 řádek (int):
```
5
```

---

## Vlastní `test.in` — minimální příklad

Vytvoř v rootu `test.in`:

```
write "ahoj svete";
int x;
x = 5;
write "x =", x;
write "1 + 2 =", 1 + 2;
```

Spusť:
```powershell
.\run .\test.in --run
```

Očekávaný výstup:
```
ahoj svete
x =5
1 + 2 =3
```

---

## Porovnání s referenčními výstupy

Reference `.out` soubory obsahují **instrukce**, které by měl generátor vyrobit (ne výstup programu).

```powershell
# Vygeneruj instrukce do souboru
.\run .\samples\PLC_t1.in > my_output.txt

# Porovnej s referencí (jen vizuálně)
fc my_output.txt samples\PLC_t1.out
```

(Drobné rozdíly v trailing whitespace jsou OK, logika musí sedět.)

---

## Build samostatně (bez spuštění)

Pokud jen chceš zkontrolovat, že kód kompiluje:

```powershell
dotnet build PLC_Project\PLC_Project.sln
```

---

## Když to nefunguje

| Problém | Řešení |
|---|---|
| `'run' is not recognized` v PowerShellu | Použij `.\run` s tečkou |
| `BUILD FAILED` | Přečti errory, oprav kód, zkus znovu |
| Program se zasekne | Pravděpodobně čeká na vstup z `read` — zadej hodnoty nebo Ctrl+C |
| `IndexingContext` neexistuje (po úpravě gramatiky) | Smaž `PLC_Project\PLC_Project\obj\`, build znovu |
