open System
open System.IO
open System.Diagnostics

// 1. Rileva il sistema operativo corrente per invocare correttamente la shell
let isWindows = OperatingSystem.IsWindows()
let shellCmd = if isWindows then "cmd.exe" else "sh"
let shellArgs = if isWindows then "/c ClangSharpPInvokeGenerator @generate.rsp" 
                               else "-c \"ClangSharpPInvokeGenerator @generate.rsp\""

// 2. Configura il processo per catturare l'output e nascondere la finestra pop-up
let info = ProcessStartInfo(shellCmd, shellArgs)
info.CreateNoWindow <- true          // Nasconde la finestra nera lampeggiante
info.UseShellExecute <- false        // Abilita il reindirizzamento dei flussi di testo
info.RedirectStandardOutput <- true  // Cattura l'output standard di ClangSharp
info.RedirectStandardError <- true   // Cattura gli errori di ClangSharp

printfn "Esecuzione di ClangSharpPInvokeGenerator in corso..."

let proc = Process.Start(info)

// 3. Legge e stampa in tempo reale i log generati da ClangSharp nella finestra principale
let output = proc.StandardOutput.ReadToEnd()
let errori = proc.StandardError.ReadToEnd()

proc.WaitForExit()

// Stampa i risultati nel terminale corrente
if not (String.IsNullOrWhiteSpace(output)) then printfn "%s" output
if not (String.IsNullOrWhiteSpace(errori)) then printfn "ERRORI:\n%s" errori

// 4. Correzione finale delle virgolette sul file C# generato
let percorsoFile = "../NativeNew/Sqlite3NativeNew.cs"
if File.Exists(percorsoFile) then
    let codice = File.ReadAllText(percorsoFile)
    let codicePulito = codice.Replace("\"SOSTITUISCIMI_LIBRERIA\"", "SQLITE_DLL")
    File.WriteAllText(percorsoFile, codicePulito)
    printfn "Fatto! Codice generato, ripulito e salvato in: %s" percorsoFile
else
    printfn "Attenzione: Il file %s non è stato trovato. Verifica i log di ClangSharp sopra." percorsoFile
