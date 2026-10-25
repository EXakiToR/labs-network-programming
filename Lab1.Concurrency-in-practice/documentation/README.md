# Laboratory work 1: Concurrency in Practice

The report is in English and follows the supplied UTM LaTeX template.

- **Authors:** Pancenco Mihail and Elciscev Andrey.
- **Supervisors:** asist. univ. Fiștic Cristofor and asist. univ. Bîtca Ernest.
- **Compiled report:** `report/main.pdf` (36 pages).
- **Editable project:** `report/main.tex`, `title_page.tex`, `sections/`, `conclusion.tex`, and `bibliography.tex`, with the original class and logo.
- **Architecture and UML:** the architecture chapter explains responsibilities, process and thread boundaries, metric ownership, the JSON/stdin protocol, and measurement scope. Editable UML class and sequence diagrams are in `report/uml/`.
- **Code:** fourteen source-linked implementation listings cover all four tasks, including all three Task 1b variants. Each includes the original source line numbers.
- **Results figures:** six composite vector figures, including time, one-thread-relative speed-up, and peak-memory plots for every task, plus the producer–consumer diagram.
- **Evidence:** `report/data/` contains real saved sessions, dataset metadata, fresh Task 1b and cold-cache USB measurements, all 14 Task 1a/1c correctness checks, and per-run output/time logs.
- **Reproduction:** `reproduce/README.md`, `lab1.sh`, `TaskOneBBenchmark.cs`, `plot_results.py`, `sync_report_code.py`, and `package_sources.py`.

The report answers all eight task questions and all six control questions, uses plural authorship, and distinguishes incorrect unsynchronised counts from correct results. Historical USB timings with unverified cache provenance are archived but excluded from the cold-cache conclusions. Each of the three cold USB runs followed a separately confirmed physical reconnect with Spotlight indexing disabled.

To build the report with a local TeX installation:

```sh
cd report
latexmk -pdf main.tex
```

To build the C# task executables, run this from the laboratory root:

```sh
bash documentation/reproduce/lab1.sh build
```

The source bundle excludes generated executables, build caches, the old course's unused screenshots, and the large input files. Keep the existing `numbers.txt` and eight part files locally to reproduce the archived dataset exactly; the dataset manifest records their sizes, line counts, and SHA-256 digests.

The report reads implementation listings from byte-identical task snapshots in `report/code/`. Run `python3 documentation/reproduce/sync_report_code.py` from the laboratory root after changing a task source; the snapshot manifest records SHA-256 hashes. This keeps the LaTeX project self-contained for sandboxed editor previews. The UML diagrams use native TikZ and can be edited without an external rendering service. Rebuild the source archive with `python3 documentation/reproduce/package_sources.py` from the laboratory root.
