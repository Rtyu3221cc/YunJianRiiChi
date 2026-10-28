import unittest
from effect_compatibility import require_supported
class ImportGate(unittest.TestCase):
    def test_basic_source_is_allowed(self):require_supported({'type':'Sprite3D','props':{'name':'root'},'child':[]},'basic')
    def test_unimplemented_animation_is_rejected(self):
        with self.assertRaisesRegex(ValueError,'controller-components'):require_supported({'type':'Sprite3D','props':{'name':'root'},'components':{'Animator':{}}},'animated')
    def test_missing_three_axis_rotation_is_not_silently_dropped(self):
        graph={'type':'ShuriKenParticle3D','props':{'name':'petals'},'customProps':{'rotationOverLifetime':{'enable':True,'angularVelocity':{'type':2,'separateAxes':True}}}}
        with self.assertRaisesRegex(ValueError,'three-axis-angular-velocity'):require_supported(graph,'petals')
if __name__=='__main__':unittest.main()
