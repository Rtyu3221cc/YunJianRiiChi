import importlib.util, pathlib, unittest

spec=importlib.util.spec_from_file_location('character_inputs',pathlib.Path(__file__).resolve().parents[1]/'scripts/merge-character-inputs.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
class CharacterInputs(unittest.TestCase):
    def setUp(self):
        self.base={'item_definition/character':[{'id':1,'sound_folder':'role_one','init_skin':101,'full_fetter_skin':102}],
                   'item_definition/skin':[{'id':101,'character_id':1,'path':'extendRes/charactor/role_one'},{'id':102,'character_id':1,'path':'extendRes/charactor/role_one_0'}]}
    def test_new_outfit_preserves_existing_outfits(self):
        result=module.merge_config(self.base,{'item_definition/skin':[{'id':103,'character_id':1,'path':'extendRes/charactor/role_one_new'}]})
        self.assertEqual([s['id']for s in result['item_definition/skin']],[101,102,103]);self.assertEqual(len(self.base['item_definition/skin']),2)
    def test_new_character_and_skin_are_merged_together(self):
        result=module.merge_config(self.base,{'item_definition/character':[{'id':2,'sound_folder':'role_two','init_skin':201,'full_fetter_skin':201}],
            'item_definition/skin':[{'id':201,'character_id':2,'path':'extendRes/charactor/role_two'}]})
        self.assertEqual(len(result['item_definition/character']),2)
    def test_existing_key_cannot_be_renamed(self):
        with self.assertRaises(AssertionError):module.merge_config(self.base,{'item_definition/character':[{'id':1,'sound_folder':'changed'}]})
    def test_skin_cannot_change_owner(self):
        with self.assertRaises(AssertionError):module.merge_config(self.base,{'item_definition/skin':[{'id':101,'character_id':2}]})
    def test_duplicate_patch_id_is_rejected(self):
        with self.assertRaises(AssertionError):module.merge_config(self.base,{'item_definition/skin':[{'id':103},{'id':103}]})
    def test_unknown_owner_is_rejected(self):
        with self.assertRaises(AssertionError):module.merge_config(self.base,{'item_definition/skin':[{'id':201,'character_id':2,'path':'extendRes/charactor/new'}]})
    def test_path_traversal_is_rejected(self):
        with self.assertRaises(AssertionError):module.merge_config(self.base,{'item_definition/skin':[{'id':101,'path':'extendRes/charactor/../escape'}]})
    def test_default_skin_must_belong_to_character(self):
        with self.assertRaises(AssertionError):module.merge_config(self.base,{'item_definition/character':[{'id':1,'init_skin':999}]})
    def test_native_numeric_default_path_is_accepted(self):
        result=module.merge_config(self.base,{'item_definition/skin':[{'id':103,'character_id':1,'path':'','assetLayout':'native'}]})
        self.assertEqual(result['item_definition/skin'][-1]['id'],103)
    def test_native_explicit_path_is_bound_to_skin_id(self):
        row={'id':103,'character_id':1,'path':'deco/character/103','assetLayout':'native'}
        self.assertEqual(module.merge_config(self.base,{'item_definition/skin':[row]})['item_definition/skin'][-1],row)
        row['path']='deco/character/104'
        with self.assertRaises(AssertionError):module.merge_config(self.base,{'item_definition/skin':[row]})
    def test_native_path_cannot_escape(self):
        with self.assertRaises(AssertionError):module.merge_config(self.base,{'item_definition/skin':[{'id':103,'character_id':1,'path':'../103','assetLayout':'native'}]})
    def test_only_original_generic_gender_skins_can_have_zero_owner(self):
        row={'id':400000,'character_id':0,'path':'extendRes/charactor/default_girl'}
        module.merge_config(self.base,{'item_definition/skin':[row]})
        row['id']=400002
        with self.assertRaises(AssertionError):module.merge_config(self.base,{'item_definition/skin':[row]})
    def test_native_v2_spine_matches_original_external_config(self):
        import json
        root=pathlib.Path(__file__).resolve().parents[1]
        catalog=json.loads((root/'unity/Assets/Resources/Majsoul/CharacterDirectory/catalog.json').read_text('utf8'))
        skins={skin['id']:skin for role in catalog['roles']for skin in role['skins']}
        for sid in [40012803,40012903,40013003,40013103]:
            with self.subTest(skin=sid):
                config=json.loads((root/f'docs/reference/november-2026/spine-v2/{sid}_spine_offset.json').read_text('utf8'))
                info=json.loads((root/f'docs/reference/november-2026/spine-v2/{sid}_info.json').read_text('utf8'))
                for runtime,original in [('spineWidth','spine_width'),('spineHeight','spine_height'),('pivotX','pivot_x'),('pivotY','pivot_y')]:
                    self.assertEqual(skins[sid][runtime],config[original])
                self.assertEqual(skins[sid]['parts'],[f'Spine/{sid}/{layer}'for layer in info['layers']])
    def test_rank_sprites_keep_original_logical_canvas(self):
        import json
        root=pathlib.Path(__file__).resolve().parents[1]
        sources=json.loads((root/'docs/reference/november-2026/rank-ui-logical-canvas-r11.json').read_text('utf8'))
        for row in sources:
            with self.subTest(sprite=row['name']):
                image=(root/f"unity/Assets/Resources/Majsoul/Settlement/RankCalc/{row['name']}.png").read_bytes()
                self.assertEqual([int.from_bytes(image[16:20],'big'),int.from_bytes(image[20:24],'big')],row['logicalSize'])
if __name__=='__main__':unittest.main()
