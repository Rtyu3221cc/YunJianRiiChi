"""Import the supplied APK's decoration UI and a small, offline preview selection."""
from pathlib import Path
import hashlib
import io
import json
import runpy
from PIL import Image

R = Path(__file__).resolve().parent.parent
S = runpy.run_path(str(R / 'scripts/study-characters-1018.py'))
OUT = R / 'unity/Assets/Resources/Majsoul/Wardrobe'
OUT.mkdir(parents=True, exist_ok=True)
records = []
ui = []

def write_image(image, relative, source, obj, border=None):
    file = OUT / (relative + '.png')
    file.parent.mkdir(parents=True, exist_ok=True)
    stream = io.BytesIO()
    image.save(stream, format='PNG')
    file.write_bytes(stream.getvalue())
    records.append(dict(output=relative, source=source, bundle=S['paths'].get(source),
                        pathID=obj.path_id, width=image.width, height=image.height,
                        sha256=hashlib.sha256(stream.getvalue()).hexdigest()))
    if border is not None:
        ui.append(dict(key=relative.removeprefix('UI/'), border=border))
    return 'Majsoul/Wardrobe/' + relative

atlas = 'myassets/ui/lobby/extend/liaoshe_extra/pic/common/atlas_lobby_liaoshe_extra_common_0.png'
for obj in S['af'](S['paths'][atlas]).objects.values():
    if obj.type.name != 'Sprite':
        continue
    data = obj.read_typetree()
    rect, offset = data['m_Rect'], data['m_RD']['textureRectOffset']
    canvas = Image.new('RGBA', (round(rect['width']), round(rect['height'])))
    image = obj.read().image
    canvas.paste(image, (round(offset['x']), round(rect['height'] - offset['y'] - image.height)))
    write_image(canvas, 'UI/' + obj.peek_name(), atlas, obj, [data['m_Border'][k] for k in 'xyzw'])

def texture(path, relative):
    obj = S['obj'](path, 'Texture2D')
    return write_image(obj.read().image, relative, path, obj)

items = []
def item(id, category, name, icon, **extra):
    items.append(dict(id=id, category=category, name=name, icon=icon, builtin=True, **extra))

# Existing cloth art keeps its original provenance in reference-skin.json.
item('cloth-chigua', 'cloth', '桌布－吃瓜', 'Majsoul/Table/cloth-chigua', texture='Majsoul/Table/cloth-chigua')
item('cloth-default', 'cloth', '默认桌布', 'Majsoul/Table/cloth', texture='Majsoul/Table/cloth')
for key, name in [('mjp_default', '默认牌背'), ('mjp_20chunjie', '牌背－新春')]:
    root = 'myassets/deco/mjpai/' + key
    icon = texture(root + '/pic/common/' + key + '.jpg', 'Items/' + key + '/icon')
    hand = texture(root + '/3d/texture/hand.png', 'Items/' + key + '/hand')
    table = texture(root + '/3d/texture/mjp.png', 'Items/' + key + '/table')
    preview = texture(root + '/preview/common/preview.png', 'Items/' + key + '/preview')
    item(key, 'tileBack', name, icon, hand=hand, table=table, preview=preview)
for key, name in [('mjpface_default', '默认牌面'), ('mjpface_2308event', '猫咪牌面')]:
    root = 'myassets/deco/mjpface/' + key
    icon = texture(root + '/pic/common/' + key + '.jpg', 'Items/' + key + '/icon')
    hand = texture(root + '/3d/texture/hand.png', 'Items/' + key + '/hand')
    table = texture(root + '/3d/texture/mjp.png', 'Items/' + key + '/table')
    preview = texture(root + '/preview/common/preview.png', 'Items/' + key + '/preview')
    item(key, 'tileFace', name, icon, hand=hand, table=table, preview=preview)
for key, name in [('headframe_default', '默认头像框'), ('headframe_catear3', '猫耳头像框')]:
    root = 'myassets/deco/head_frame/' + key
    frame = texture(root + '/icon/common/' + key + '.png', 'Items/' + key + '/frame')
    icon = texture(root + '/pic/common/' + key + '.jpg', 'Items/' + key + '/icon')
    item(key, 'headFrame', name, icon, texture=frame,
         avatarWindow=[55 / 320, 55 / 320, 210 / 320, 210 / 320] if key == 'headframe_catear3' else [0, 0, 1, 1])
item('background-yard', 'lobbyBackground', '庭院', 'Majsoul/Lobby/yard', texture='Majsoul/Lobby/yard')
item('background-indoor', 'lobbyBackground', '宿舍', 'Majsoul/Lobby/indoor', texture='Majsoul/Lobby/indoor')
catalog = dict(schema=1, ui=ui, items=items)
(OUT / 'catalog.json').write_text(json.dumps(catalog, ensure_ascii=False, indent=2) + '\n', 'utf8')
(OUT / 'provenance.json').write_text(json.dumps(dict(apk='Majsoul_4.0.16_MC.apk', records=records), ensure_ascii=False, indent=2) + '\n', 'utf8')
print('Imported decoration preview:', len(ui), 'native UI sprites;', len(items), 'items;', len(records), 'APK images', flush=True)
