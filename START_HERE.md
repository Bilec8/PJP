# START HERE — kickoff prompt for Claude Code

Copy-paste this as your **first message** to Claude Code after `cd`-ing into the project directory:

---

I'm working on the PLC (Programming Languages and Compilers) semester project at VŠB-TUO. Read `PROJECT_BRIEF.md` in this directory carefully — it has the full assignment, language spec, target instruction set, recommended structure, and an 8-phase implementation plan.

Then:

1. List the contents of `original_labs/` so I can see what reference material is available.
2. Look at `samples/` (if it exists) and tell me whether the `.out` files use uppercase or lowercase instructions — this decides our convention.
3. Confirm you understand the four compiler phases (parse → type-check → codegen → VM) and which lab provides what.
4. Propose your starting point (should be Phase 1 from the brief: project scaffold).

Don't write any code yet. Just orient yourself, read the brief, and confirm the plan. I'll give you the green light to start Phase 1.

Communicate in Czech with me; English is fine for code/comments.

---

## Files to provide to Claude Code

Place these in your working directory before starting the session:

```
your-project-folder/
├── PROJECT_BRIEF.md            # the brief (mandatory, Claude Code reads first)
├── original_labs/
│   ├── Lab8/
│   │   ├── PLC_Lab8_expr.g4
│   │   ├── PLC_Lab8.csproj
│   │   ├── Program.cs
│   │   ├── Type.cs
│   │   ├── SymbolTable.cs
│   │   ├── Errors.cs
│   │   ├── VerboseListener.cs
│   │   ├── TypeCheckingListener.cs
│   │   ├── TypeCheckingVisitor.cs
│   │   └── input.txt
│   ├── Lab9/
│   │   ├── PLC_Lab9_expr.g4
│   │   ├── PLC_Lab9.csproj
│   │   ├── Program.cs
│   │   ├── Type.cs
│   │   ├── SymbolTable.cs
│   │   ├── Errors.cs
│   │   ├── VerboseErrorListener.cs
│   │   ├── TypeCheckingListener.cs
│   │   ├── CodeGeneratorListener.cs
│   │   ├── CodeGeneratorVisitor.cs
│   │   └── input.txt
│   └── Lab10/
│       ├── PLC_Lab10.sln
│       ├── PLC_Lab10.csproj
│       ├── Program.cs
│       ├── VirtualMachine.cs
│       └── input.txt
└── samples/                    # download from wiki, see brief for URLs
    ├── PLC_t1.in
    ├── PLC_t1.out
    ├── PLC_t2.in
    ├── PLC_t2.out
    ├── PLC_t3.in
    ├── PLC_t3.out
    └── PLC_errors.in
```

## How to download the samples (PowerShell)

```powershell
mkdir samples
cd samples
$base = "http://linedu.vsb.cz/~beh01/wiki_data"
@("PLC_t1.in","PLC_t1.out","PLC_t2.in","PLC_t2.out","PLC_t3.in","PLC_t3.out","PLC_errors.in") | ForEach-Object {
    Invoke-WebRequest -Uri "$base/$_" -OutFile $_
}
cd ..
```

## What to do during the session

After the kickoff message, expect Claude Code to:

1. Confirm understanding and propose Phase 1
2. You say "go ahead with Phase 1"
3. It scaffolds the project, you verify `dotnet build` works
4. Continue phase by phase, building and testing after each
5. Phases 4 and 6 are the densest — expect those to take the longest

If something goes off the rails, you can say:
- "Stop and re-read PROJECT_BRIEF.md, you've drifted from the plan"
- "Show me the diff between your generated output for PLC_t1.in and the expected PLC_t1.out"
- "Explain what `<X>` does and why" — useful before defending the project

## When the project is done

For oral defense prep, ask Claude Code to walk you through:
- The grammar (each rule, why it's structured that way)
- How operator precedence is encoded
- Listener vs visitor decision and why each fits where it does
- Label-based control flow code generation for `if`/`while`
- The VM's instruction loop and label pre-scanning

You should be able to explain all of these without notes before submitting.
