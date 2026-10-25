# Reproducing Lab 1

Run commands from `Lab1.Concurrency-in-practice`.

```sh
bash documentation/reproduce/lab1.sh build
bash documentation/reproduce/lab1.sh task1a 8
bash documentation/reproduce/lab1.sh task1b 8 Unsynchronized 1
bash documentation/reproduce/lab1.sh task1b 8 Locked 1
bash documentation/reproduce/lab1.sh task1b 8 LocalCounters 1
bash documentation/reproduce/lab1.sh task1c 2
bash documentation/reproduce/lab1.sh task2 1 /Volumes/ESD-USB
```

Tasks 1a and 1c use 1, 2, 4, 8, 16, 32, and 64 workers. Task 1b uses the runtime's logical processor count (8 on the measured machine); repeat Unsynchronized three times. Its additional one-thread references are supplementary experiments. The benchmark loads the array and collects garbage before starting its counting stopwatch. External process peak RSS includes the loading phase.

For Task 2, copy the eight ordered part files to the drive first, disable Spotlight indexing on that volume, and safely eject, physically unplug, and reconnect before **each** of the 1-, 2-, and 4-worker measurements. Do not preview, hash, or read input files after reconnecting and before timing. Running several configurations without reconnecting does not reproduce the cold-cache experiment.

Example for whole-process time and memory, after building:

```sh
cd code
/usr/bin/time -l -o task1a-8.time.txt dotnet artifacts/TaskOneA/TaskOneA.dll 8
```

For Task 1b, use its printed `ELAPSED_MS` for counting time and the process-wide `maximum resident set size` for memory. Loading is excluded from the former and included in the latter. On macOS, the external time tool reports peak RSS in bytes.

The report's primary Task 1a/1c figures reproduce the saved 16:42 session; the 15:43 session is shown as a repeatability check. Task 1b supplementary runs and the three cold-cache USB runs were measured separately for this report. Raw evidence is in `documentation/report/data`. The unverified historical Task 2 menu measurements are retained for provenance but excluded from cold-cache conclusions.

Regenerate report tables and vector figures using the supplied `plot_results.py` (Python, matplotlib, and numpy required). Compile `documentation/report/main.tex` with the supplied `tum_report.cls`, logo, sections, and figure files present.
