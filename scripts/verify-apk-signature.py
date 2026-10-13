"""Verify the final APK, including v1 on installers that do not select v2."""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
from zipfile import ZipFile

CERTIFICATE = 'd1d8e506c6d3b62e9610dce294c07008057318fa9bafae702fb8b01f35f0dc2f'

def verify_signature(apk):
    apk = Path(apk).resolve()
    root = Path(__file__).resolve().parent.parent
    with ZipFile(apk) as archive:
        assert archive.testzip() is None, 'APK ZIP is incomplete or damaged'
        names = archive.namelist()
        assert len(names) == len(set(names)), 'Duplicate APK ZIP entries'
        assert 'META-INF/MANIFEST.MF' in names, 'Missing v1/JAR signature manifest'
        pairs = [n for n in names if re.fullmatch(r'META-INF/[^/]+\.SF', n)]
        assert any(n[:-3]+suffix in names for n in pairs for suffix in ('.RSA','.DSA','.EC')), 'Missing v1 signature/certificate pair'
    unity_java = Path('D:/Unity/Lib/6000.6.4f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe')
    java = os.environ.get('RIICHI_JAVA') or (str(Path(os.environ['JAVA_HOME'])/'bin'/('java.exe' if os.name=='nt' else 'java')) if os.environ.get('JAVA_HOME') else None) or shutil.which('java') or str(unity_java)
    jar = Path(os.environ.get('RIICHI_APKSIGNER_JAR', str(root/'tools/sdk/android-15/lib/apksigner.jar')))
    align = os.environ.get('RIICHI_ZIPALIGN') or str(root/'tools/sdk/android-15/zipalign.exe')
    assert Path(java).is_file() and jar.is_file() and Path(align).is_file(), 'Signing verification tools unavailable; set RIICHI_JAVA, RIICHI_APKSIGNER_JAR and RIICHI_ZIPALIGN'
    def verify(extra):
        result = subprocess.run([java,'-jar',str(jar),'verify','--verbose','--print-certs',*extra,str(apk)], capture_output=True, text=True, encoding='utf8', errors='replace')
        assert result.returncode == 0, 'Cryptographic APK verification failed: '+(result.stderr+result.stdout)[:1800]
        return result.stdout
    full = verify(['--min-sdk-version','23'])
    assert 'Verified using v1 scheme (JAR signing): true' in full, 'v1 signature did not verify'
    assert 'Verified using v2 scheme (APK Signature Scheme v2): true' in full, 'v2 signature did not verify'
    v1 = verify(['--min-sdk-version','23','--max-sdk-version','23'])
    assert 'Verified using v1 scheme (JAR signing): true' in v1, 'Independent v1 verification failed'
    certificate = re.search(r'Signer #1 certificate SHA-256 digest: ([0-9a-f]+)', full)
    assert certificate and certificate[1] == CERTIFICATE, 'APK signing certificate changed'
    result = subprocess.run([align,'-c','-P','16','4',str(apk)],capture_output=True)
    assert result.returncode == 0, 'APK/native-library 16 KB alignment failed'
    return {'file':apk.name,'bytes':apk.stat().st_size,'sha256':hashlib.sha256(apk.read_bytes()).hexdigest(),'v1':True,'v2':True,'independentV1':True,'certificateSHA256':certificate[1],'alignment16KB':True}

if __name__ == '__main__':
    print(json.dumps(verify_signature(sys.argv[1]),indent=2))
