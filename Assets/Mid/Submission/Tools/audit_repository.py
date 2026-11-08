from pathlib import Path
import subprocess, re, json, sys
ROOT=Path(__file__).resolve().parents[4]
OUT=ROOT/'Assets/Mid/Submission'
def git(*args):return subprocess.check_output(['git','-c','core.quotepath=false',*args],cwd=ROOT).decode('utf-8').strip()
missing=[]
for path in (ROOT/'Assets/Mid').rglob('*'):
    if path.name.endswith('.meta') or path.name.startswith('.'):continue
    if not Path(str(path)+'.meta').exists():missing.append(path.relative_to(ROOT).as_posix())
tracked_generated=git('ls-files','Library','Temp','Logs','UserSettings').splitlines()
guid_rows=subprocess.check_output(['rg','--no-filename','--no-heading','^guid:','Assets','Library/PackageCache','-g','*.meta'],cwd=ROOT,timeout=60).decode('utf-8')
known=set(re.findall(r'guid:\s*([a-f0-9]{32})',guid_rows))
unresolved=[]
for path in (ROOT/'Assets/Mid').rglob('*'):
    if path.suffix.lower() not in ['.unity','.prefab','.mat','.asset']:continue
    for guid in set(re.findall(r'guid:\s*([a-f0-9]{32})',path.read_text(encoding='utf-8',errors='ignore'))):
        if guid.startswith('0000000000000000'):continue
        if guid not in known:unresolved.append(dict(file=path.relative_to(ROOT).as_posix(),guid=guid))
source_paths=[]
for name in ['Scenes','Scripts','Materials','Shaders','Prefabs']:
    source_paths += [p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/Mid'/name).rglob('*') if p.is_file()]
data=dict(branch=git('branch','--show-current'),head=git('rev-parse','HEAD'),remote=git('remote','get-url','origin'),
          missing_meta=missing,unresolved_asset_guids=unresolved,tracked_generated_folders=tracked_generated,
          source_file_count=len(source_paths),source_paths=sorted(source_paths),
          status_short=git('status','--short'),publication_status='NOT PUSHED BY AGENT')
(OUT/'REPOSITORY_AUDIT.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
(OUT/'GIT_FILES.txt').write_text('\n'.join(sorted(source_paths))+'\n',encoding='utf-8')
sys.stdout.reconfigure(encoding='utf-8');print(json.dumps({k:v for k,v in data.items() if k not in ['source_paths','status_short']},ensure_ascii=False,indent=2))
