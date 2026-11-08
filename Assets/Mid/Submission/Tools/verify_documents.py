from pathlib import Path
import sys, json, hashlib
ROOT=Path(__file__).resolve().parents[4]
OUT=ROOT/'Assets/Mid/Submission'
sys.path.insert(0,str(ROOT/'Logs/MidTools/pypdf.whl'))
from pypdf import PdfReader
pdf=PdfReader(OUT/'The_Silent_Pavilion_Midterm.pdf')
text='\n'.join(page.extract_text() for page in pdf.pages)
assert len(pdf.pages)==3, 'Expected 3 pages'
for term in ['The Silent Pavilion','MidMeshBuilder','MidWispMotion','기말','오브젝트']:
    assert term in text, 'Missing content: '+term
images=sum(len(page.images) for page in pdf.pages)
assert images==2, f'Expected 2 actual screenshots, found {images}'
data=dict(status='PASS',pages=len(pdf.pages),embedded_images=images,korean_text_verified=True,
          rendered_preview='Logs/MidPdfPreview/Page_1.png .. Page_3.png',
          sha256=hashlib.sha256((OUT/'The_Silent_Pavilion_Midterm.pdf').read_bytes()).hexdigest())
(OUT/'PDF_VERIFICATION.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(data,ensure_ascii=True))
