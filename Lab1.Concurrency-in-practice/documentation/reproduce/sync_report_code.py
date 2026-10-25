"""Copy the exact task sources into the LaTeX project for sandboxed compilers."""
from pathlib import Path
import hashlib
import json
import shutil

root = Path(__file__).resolve().parents[2]
output = root / 'documentation/report/code'
output.mkdir(exist_ok=True)
manifest = []
for name in ['TaskOneA.cs', 'TaskOneB.cs', 'TaskOneC.cs', 'TaskTwo.cs']:
    source = root / 'code' / name
    destination = output / name
    shutil.copy2(source, destination)
    assert source.read_bytes() == destination.read_bytes()
    manifest.append({'file': name, 'source': 'code/' + name,
                     'sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                     'bytes': source.stat().st_size})
(output / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
print('Verified four byte-identical report-local task source snapshots.')
