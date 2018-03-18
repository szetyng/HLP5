# For Sze Tyng Lee

## To do
1. in `multiParseLine`, should we start with symTab = None and address = 0u, ie not having input parametes for these at all? 
2. Finalise type of multiline ASM instructions we are accepting: array of lines or a single long string?
3. Lots of tests are using `parseLine`, so should it be removed?
4. All uppercase in parse!!
5. How to do shifted regs in SingleR: parse it in the module (look at changing op2 stuff), then call Shift.execute? **Remember to add type for this**
6. Remove zero offset

## Observations
If there are multiple labels with the same name, current code rewrites old label value