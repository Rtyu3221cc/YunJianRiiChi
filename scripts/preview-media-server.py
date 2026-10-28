"""Loopback-only preview server with byte ranges for exact video seeking."""
from http.server import SimpleHTTPRequestHandler,ThreadingHTTPServer
from pathlib import Path
from functools import partial
import argparse,re

class PreviewHandler(SimpleHTTPRequestHandler):
    def send_head(self):
        self.remaining=None
        path=Path(self.translate_path(self.path))
        if path.suffix.lower()!='.mp4' or not path.is_file():return super().send_head()
        file=path.open('rb');size=path.stat().st_size;start,end=0,size-1;header=self.headers.get('Range');status=200
        if header:
            match=re.fullmatch(r'bytes=(\d*)-(\d*)',header.strip())
            if not match or not any(match.groups()):file.close();self.send_error(416,'Unsupported range');return None
            first,last=match.groups()
            if first:start=int(first);end=min(end,int(last))if last else end
            else:start=max(0,size-int(last))
            if start>=size or end<start:file.close();self.send_error(416,'Invalid range');return None
            status=206
        self.send_response(status);self.send_header('Content-Type','video/mp4');self.send_header('Accept-Ranges','bytes');self.send_header('Content-Length',str(end-start+1));self.send_header('Cache-Control','no-store')
        if status==206:self.send_header('Content-Range',f'bytes {start}-{end}/{size}')
        self.end_headers();file.seek(start);self.remaining=end-start+1;return file
    def copyfile(self,source,outputfile):
        if self.remaining is None:return super().copyfile(source,outputfile)
        try:
            while self.remaining:
                chunk=source.read(min(1024*1024,self.remaining))
                if not chunk:break
                outputfile.write(chunk);self.remaining-=len(chunk)
        except (BrokenPipeError,ConnectionResetError):pass

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--port',type=int,default=8875);args=parser.parse_args()
    root=Path(__file__).resolve().parent.parent/'dist/previews/2026-10-27-effects-reference'
    ThreadingHTTPServer(('127.0.0.1',args.port),partial(PreviewHandler,directory=str(root))).serve_forever()
