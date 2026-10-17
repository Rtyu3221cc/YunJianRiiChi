"""Build 0.5.45 source/release ZIPs and verify the actual APK ZIP copy.

This script writes dist/build artifacts only. It does not touch Git or GitHub.
"""
import hashlib
from importlib import import_module
import importlib.util
import json
from pathlib import Path
import shutil
import sys
from zipfile import ZipFile, ZIP_DEFLATED

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('prepare0545', ROOT / 'scripts/prepare-github-0545.py')
prepare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(prepare)


def main():
    signature, report = prepare.verify_artifacts()
    dist = prepare.DIST
    source_zip = dist / 'yunjian-riichi-source-0.5.45.zip'
    release_zip = dist / 'yunjian-riichi-unity-release-0.5.45.zip'
    handoff_copy = dist / 'handoff-unity-0.5.45.md'
    shutil.copyfile(prepare.HANDOFF, handoff_copy)
    source_rows = prepare.check_files(prepare.source_paths())
    with ZipFile(source_zip, 'w', ZIP_DEFLATED, compresslevel=9) as archive:
        for row in source_rows:
            file = ROOT / row['path']
            assert prepare.sha256(file) == row['sha256'], 'Source changed while packaging: ' + row['path']
            archive.write(file, 'YunJianRiichi/' + row['path'])
    files = [prepare.APK, prepare.CHECKSUM, prepare.VERIFICATION, prepare.NOTES,
             handoff_copy, ROOT / 'README.md', ROOT / 'ARTWORK.md', *prepare.preview_paths()]
    with ZipFile(release_zip, 'w', ZIP_DEFLATED, compresslevel=9) as archive:
        for file in sorted(files):
            relative = file.relative_to(dist).as_posix() if file.is_relative_to(dist) else file.name
            archive.write(file, relative)
    extracted = ROOT / 'build/zip-apk-proof-0545' / prepare.APK.name
    extracted.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(release_zip) as archive:
        assert archive.testzip() is None, 'Release ZIP corruption'
        assert len(archive.namelist()) == len(set(archive.namelist())), 'Duplicate release ZIP entries'
        assert archive.getinfo(prepare.APK.name).file_size == signature['bytes']
        with archive.open(prepare.APK.name) as src, extracted.open('wb') as dst:
            shutil.copyfileobj(src, dst)
    sys.path.insert(0, str(ROOT / 'scripts'))
    zip_signature = import_module('verify-apk-signature').verify_signature(extracted)
    assert zip_signature['bytes'] == signature['bytes'] and zip_signature['sha256'] == signature['sha256'], 'Release ZIP APK differs from final APK'
    with ZipFile(source_zip) as archive:
        assert archive.testzip() is None, 'Source ZIP corruption'
        names = archive.namelist()
        assert len(names) == len(set(names))
        for name in names:
            relative = Path(name)
            assert not set(relative.parts).intersection(prepare.EXCLUDED_PARTS), 'Forbidden ZIP path'
            assert relative.suffix.lower() not in prepare.EXCLUDED_SUFFIXES | {'.apk'}, 'Forbidden ZIP file'
    assets = []
    for path in [prepare.APK, prepare.CHECKSUM, prepare.VERIFICATION, prepare.NOTES,
                 source_zip, release_zip, handoff_copy]:
        assert path.stat().st_size <= prepare.RELEASE_LIMIT, 'Release asset exceeds GitHub size limit: ' + path.name
        assets.append({'file': path.name, 'bytes': path.stat().st_size, 'sha256': prepare.sha256(path)})
    output = {'version': prepare.VERSION, 'apk': signature, 'releaseZipApk': zip_signature,
              'sourceFiles': len(source_rows), 'previewPngs': len(list(prepare.PREVIEW.rglob('*.png'))),
              'previewMp4s': len(list(prepare.PREVIEW.rglob('*.mp4'))), 'assets': assets}
    (ROOT / 'build/release-package-0545.json').write_text(json.dumps(output, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
    (ROOT / 'build/publish-release-body-0545.md').write_text(prepare.release_body(signature, report), encoding='utf8')
    print(json.dumps(output, ensure_ascii=False, indent=2))
    print('PASS: curated source ZIP and complete release ZIP; extracted APK repeats ZIP/v1/v2/API23-only-v1/certificate/16KB verification.')


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print(type(error).__name__ + ': ' + str(error), file=sys.stderr)
        sys.exit(1)
