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
if __name__=='__main__':unittest.main()
