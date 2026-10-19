"""Decode the official self-describing lqc table; media always come from APK."""
from pathlib import Path
import struct,json,urllib.request,concurrent.futures
R=Path(__file__).resolve().parent.parent;O=R/'build/characters-1018'
def var(b,i):
 v=s=0
 while True:
  n=b[i];i+=1;v|=(n&127)<<s
  if n<128:return v,i
  s+=7
def msg(b):
 i=0;d={}
 while i<len(b):
  tag,i=var(b,i);n,w=tag>>3,tag&7
  if w==0:v,i=var(b,i)
  elif w==2:l,i=var(b,i);v=b[i:i+l];i+=l
  elif w==5:v=b[i:i+4];i+=4
  elif w==1:v=b[i:i+8];i+=8
  else:raise ValueError((i,n,w))
  d.setdefault(n,[]).append(v)
 return d
def st(d,n):return d.get(n,[b''])[0].decode('utf8')
def main():
 root=msg((O/'lqc.lqbin').read_bytes());schema={};result={}
 for tb in root[3]:
  t=msg(tb);tn=st(t,1)
  for sb in t.get(2,[]):
   s=msg(sb);name=st(s,1);fields=[]
   for fb in s.get(3,[]):
    f=msg(fb);fields.append(dict(name=st(f,1),type=st(f,3),index=f.get(4,[0])[0],array=f.get(2,[0])[0]))
   schema[tn+'/'+name]=fields
 for db in root[4]:
  d=msg(db);key=st(d,1)+'/'+st(d,2)
  if not key.startswith(('item_definition/character','item_definition/skin','character/','voice/','item_definition/source_limit')):continue
  values=[]
  for rb in d.get(3,[]):
   row=msg(rb);v={}
   for f in schema[key]:
    a=row.get(f['index'],[]);typ=f['type']
    if not a:continue
    def dec(x):
     if typ=='string':return x.decode('utf8')
     if typ=='float':return struct.unpack('<f',x)[0]
     if typ=='double':return struct.unpack('<d',x)[0]
     if isinstance(x,bytes):
      packed=[];i=0
      while i<len(x):n,i=var(x,i);packed.append(n)
      return packed
     return x
    a=[dec(x)for x in a];v[f['name']]=a if f['array'] else a[0]
   values.append(v)
  result[key]=values
 (O/'config-schema.json').write_text(json.dumps(schema,ensure_ascii=False,indent=2),encoding='utf8')
 (O/'config.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
 for k,v in result.items():print(k,len(v),str(v[0])[:500]if v else'')
if __name__=='__main__':main()
