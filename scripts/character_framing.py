"""Compile illustration poses into the Unity APK's anchored, positive-up space.

The web client's default offsets describe different parents and MUST NOT be used
here. F0 is the APK skin-card's explicit 228px force-offset override.
"""
SCENES = ('A0', 'B0', 'B1', 'B2', 'F0', 'I0', 'J0', 'K0')


def placements(item, data, defaults):
    result = []
    if not data:
        return result
    for scene in SCENES:
        for index, default in enumerate(defaults[scene]):
            poses = data.get(scene) or data.get(scene[0] + '0')
            if not poses:
                continue
            pose = poses[index] if isinstance(poses, list) else poses
            width, height = default['scale'] * data['w'], default['scale'] * data['h']
            sign = -1 if pose['reverse'] and not item['lock_reverse'] else 1
            x = default['x'] + pose['x'] * width * pose['scale']
            y = default['y'] + pose['y'] * height * pose['scale']
            if sign < 0:
                x += 2 * data['center'] * width
            offset = (dict(x=110, y=-234, scaleX=1.58, scaleY=1.58)
                      if scene == 'F0' else default.get('offset', dict(x=0, y=0, scaleX=1, scaleY=1)))
            sx, sy = offset['scaleX'], offset['scaleY']
            result.append(dict(scene=scene, index=index,
                rect=[offset['x'] + x / sx, offset['y'] + y / sy,
                      width / abs(sx) * pose['scale'], height / abs(sy) * pose['scale']],
                flip=sign * (1 if sx > 0 else -1) < 0,
                reverseArt=sign < 0 and bool(item['no_reverse'])))
    return result
