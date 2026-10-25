"""Package the editable report, UML diagrams, task code, and retained evidence."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parents[2]
report = root / 'documentation/report'
bundle = root / 'documentation/lab1-report-sources.zip'
files = list((root / 'code').glob('*.cs'))
files += [root / 'documentation/README.md', root / 'Lab 1 - Concurrency in practice.pdf']
for suffix in ('*.md', '*.py', '*.sh', '*.cs'):
    files += list(Path(__file__).parent.glob(suffix))
files += list(report.glob('*.tex')) + list(report.glob('*.cls')) + [report / 'UTM_Logo.png']
for folder in ('sections', 'uml'):
    files += list((report / folder).glob('*.tex'))
files += list((report / 'figures').glob('*.pdf'))
files += list((report / 'code').glob('*.cs')) + list((report / 'code').glob('*.json'))
files += [path for path in (report / 'data').iterdir()
          if path.suffix in {'.json', '.csv', '.tex', '.txt', '.html', '.md'}]
with ZipFile(bundle, 'w', ZIP_DEFLATED) as archive:
    for path in sorted(set(files)):
        archive.write(path, path.relative_to(root.parent))
    archive.writestr('README.md',
        'Lab 1 source bundle. Open Lab1.Concurrency-in-practice/documentation/README.md. '
        'The compiled report is delivered separately. Large datasets are excluded.\n')
with ZipFile(bundle) as archive:
    assert archive.testzip() is None
    assert any(path.endswith('/uml/classes.tex') for path in archive.namelist())
    assert any(path.endswith('/uml/sequence.tex') for path in archive.namelist())
    print(f'Validated {len(archive.namelist())} files in {bundle.name}.')
