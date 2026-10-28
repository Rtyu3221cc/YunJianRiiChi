"""Import-time rejection of source features that the current adapter cannot play."""
def unsupported_features(graph):
    issues=[]
    def visit(node,path=''):
        path=path+'/'+node.get('props',{}).get('name','?')
        if node.get('components'):issues.append((path,'controller-components',list(node['components'])))
        if node.get('type')=='ShuriKenParticle3D':
            c=node['customProps']
            for prop in ['Lifetime','Speed','Size','Rotation','Color']:
                if c.get('start'+prop+'Type',0)not in [0,2]:issues.append((path,'start'+prop+'Type',c['start'+prop+'Type']))
            if c.get('startDelayType',0)!=0:issues.append((path,'random-start-delay',c['startDelayType']))
            if c.get('randomizeRotationDirection',0)!=0:issues.append((path,'random-rotation-direction',c['randomizeRotationDirection']))
            for name,field,allowed in [('colorOverLifetime','color',[1]),('sizeOverLifetime','size',[0]),('velocityOverLifetime','velocity',[0,1,2]),('rotationOverLifetime','angularVelocity',[0,2])]:
                module=c.get(name,{})
                if module.get('enable'):
                    value=module.get(field,{});kind=value.get('type',0)
                    if kind not in allowed:issues.append((path,name+'-type',kind))
                    if name=='rotationOverLifetime'and value.get('separateAxes'):issues.append((path,'three-axis-angular-velocity','not imported'))
            sheet=c.get('textureSheetAnimation',{})
            if sheet.get('enable'):
                if sheet.get('type',0)!=0:issues.append((path,'single-row-texture-sheet',sheet['type']))
                if sheet.get('startFrame',{}).get('type',0)!=0:issues.append((path,'random-start-frame',sheet['startFrame']['type']))
        for child in node.get('child',[]):visit(child,path)
    visit(graph)
    return issues

def require_supported(graph,label):
    issues=unsupported_features(graph)
    if issues:raise ValueError('Source effect has unsupported behavior; do not silently import '+label+': '+str(issues[:8]))
