"""Record only the user-requested game preview window; never inject UI input."""
from pathlib import Path
import argparse, json, sys, time
from fractions import Fraction

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / 'tools/video-inspector'))
import av


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--seconds', type=float, default=20)
    parser.add_argument('--title', default='雀魂麻將 - 个人 - Microsoft\u200b Edge')
    parser.add_argument('--window-id', type=int)
    parser.add_argument('--desktop-region', action='store_true')
    parser.add_argument('--right-viewport-canvas',nargs=4,type=int,metavar=('VIEW_WIDTH','VIEW_HEIGHT','CANVAS_TOP','CANVAS_HEIGHT'))
    parser.add_argument('--source-label',default='Edge original game preview window')
    args = parser.parse_args()
    output = args.output.resolve()
    allowed = (ROOT / 'dist/previews/2026-10-27-effects-reference').resolve()
    if not output.is_relative_to(allowed) or output.suffix != '.mp4':
        raise ValueError('Reference recordings must stay in the named preview directory')
    output.parent.mkdir(parents=True, exist_ok=True)
    started = time.time()
    count = 0
    device = 'hwnd=' + str(args.window_id) if args.window_id else 'title=' + args.title
    options = {'framerate': '30', 'draw_mouse': '0'}
    if args.desktop_region:
        # Record just this window's visible client rectangle. This uses no input
        # injection and is needed for GPU-composited browser windows.
        import ctypes
        from ctypes import wintypes
        rect, origin = wintypes.RECT(), wintypes.POINT(0, 0)
        user = ctypes.windll.user32
        user.SetProcessDPIAware()
        if not user.GetClientRect(args.window_id, ctypes.byref(rect)) or not user.ClientToScreen(args.window_id, ctypes.byref(origin)):
            raise RuntimeError('The selected game window is no longer available')
        x,y,width,height=0,0,rect.right,rect.bottom
        if args.right_viewport_canvas:
            vw,vh,top,height=args.right_viewport_canvas;x=rect.right-vw;y=rect.bottom-vh+top;width=vw
            if min(x,y)<0 or x+width>rect.right or y+height>rect.bottom:raise ValueError('Canvas crop is outside the selected window')
        width=width//2*2;height=height//2*2
        options.update(offset_x=str(origin.x+x), offset_y=str(origin.y+y),video_size=f'{width}x{height}')
        device = 'desktop'
    with av.open(device, format='gdigrab', options=options) as source, av.open(str(output), 'w', options={'movflags': '+faststart'}) as target:
        stream = None
        for frame in source.decode(video=0):
            if stream is None:
                width, height = frame.width // 2 * 2, frame.height // 2 * 2
                stream = target.add_stream('libx264', rate=30)
                stream.width, stream.height = width, height
                stream.pix_fmt = 'yuv420p'
                stream.options = {'crf': '20', 'preset': 'veryfast'}
                stream.thread_count = 2
                print(json.dumps({'recording': str(output), 'width': width, 'height': height}), flush=True)
            frame = frame.reformat(width=width, height=height, format='yuv420p')
            frame.pict_type=av.video.frame.PictureType.NONE
            frame.pts, frame.time_base = count, Fraction(1, 30)
            for packet in stream.encode(frame):
                target.mux(packet)
            count += 1
            if count >= round(args.seconds * 30):
                break
        for packet in stream.encode():
            target.mux(packet)
    report = dict(file=output.name, fps=30, frames=count, duration=count/30,
                  wallSeconds=time.time()-started, capturedAtUnix=started,
                  source=args.source_label, audio=False,
                  note='Original preview recording. No game account state is modified.')
    output.with_suffix('.capture.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', 'utf8')
    print(json.dumps(report, ensure_ascii=False), flush=True)


if __name__ == '__main__':
    main()
