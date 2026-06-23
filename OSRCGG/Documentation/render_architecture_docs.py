# -*- coding: utf-8 -*-
from pathlib import Path
import os
import shutil
import subprocess
import sys
import tempfile


ROOT = Path(__file__).resolve().parent
DOC_DIR = ROOT / "Architecture"
RENDER_DIR = DOC_DIR / "_rendered"
MAX_RENDER_WIDTH = 1600
MAX_RENDER_HEIGHT = 2000


def ensure_soffice_on_path():
    if shutil.which("soffice"):
        return

    candidates = []
    if sys.platform == "win32":
        for env_name in ("ProgramFiles", "ProgramFiles(x86)"):
            base = os.environ.get(env_name)
            if base:
                candidates.append(Path(base) / "LibreOffice" / "program" / "soffice.exe")

    for candidate in candidates:
        if candidate.exists():
            os.environ["PATH"] = str(candidate.parent) + os.pathsep + os.environ.get("PATH", "")
            return


def convert_to_pdf(docx_path, pdf_dir):
    ensure_soffice_on_path()
    soffice = shutil.which("soffice")
    if not soffice:
        raise FileNotFoundError("LibreOffice/soffice was not found")

    with tempfile.TemporaryDirectory(prefix="acks_soffice_profile_") as profile_dir:
        profile_uri = Path(profile_dir).resolve().as_uri()
        cmd = [
            soffice,
            "-env:UserInstallation=" + profile_uri,
            "--invisible",
            "--headless",
            "--norestore",
            "--convert-to",
            "pdf",
            "--outdir",
            str(pdf_dir),
            str(docx_path),
        ]
        result = subprocess.run(
            cmd,
            check=False,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            timeout=90,
        )

    pdf_path = pdf_dir / (docx_path.stem + ".pdf")
    if pdf_path.exists() and pdf_path.stat().st_size > 0:
        return pdf_path

    emitted = sorted(pdf_dir.glob("*.pdf"))
    if emitted:
        return emitted[0]

    raise RuntimeError(
        "LibreOffice did not produce PDF.\n"
        + result.stdout.strip()
        + "\n"
        + result.stderr.strip()
    )


def render_pdf_to_png(pdf_path, out_dir):
    try:
        import pypdfium2 as pdfium
    except ImportError as exc:
        raise RuntimeError(
            "pypdfium2 is not available; install Poppler or use the bundled Codex runtime"
        ) from exc

    # Рендерим через pdfium, чтобы проверка DOCX не зависела от внешнего Poppler.
    for old_page in out_dir.glob("page-*.png"):
        old_page.unlink()

    pdf = pdfium.PdfDocument(str(pdf_path))
    try:
        for page_index in range(len(pdf)):
            page = pdf[page_index]
            try:
                width, height = page.get_size()
                scale = min(MAX_RENDER_WIDTH / width, MAX_RENDER_HEIGHT / height)
                image = page.render(scale=scale).to_pil()
                image.save(out_dir / f"page-{page_index + 1}.png")
            finally:
                page.close()
    finally:
        pdf.close()


def render_docx(docx_path, out_dir):
    out_dir.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="acks_docx_pdf_") as pdf_tmp:
        pdf_path = convert_to_pdf(docx_path, Path(pdf_tmp))
        render_pdf_to_png(pdf_path, out_dir)


def main():
    RENDER_DIR.mkdir(parents=True, exist_ok=True)
    docx_files = sorted(p for p in DOC_DIR.glob("*.docx") if not p.name.startswith("~$"))
    for docx_path in docx_files:
        out_dir = RENDER_DIR / docx_path.stem
        render_docx(docx_path, out_dir)
        print(out_dir)


if __name__ == "__main__":
    main()
