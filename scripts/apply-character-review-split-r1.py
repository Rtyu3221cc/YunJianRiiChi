from pathlib import Path
R=Path(__file__).resolve().parent.parent
p=R/'unity/Assets/Scripts/LobbyController.Characters.cs';s=p.read_text('utf8')
start=s.index('        void CharacterHeader(');end=s.index('        void DrawRoleSelection(){',start)
s=s[:start]+s[end:]
s=s.replace('characterTransition=Now-1;}','characterTransition=Now-1;ReviewSkinIndex(skinIndex,false);ReviewSettleDorm();}',1)
s=s.replace('public void PreviewSkin(int index){skinIndex=index;characterTransition=Now-1;}','public void PreviewSkin(int index){ReviewSkinIndex(index,false);characterTransition=Now-1;}')
s=s.replace('public void PreviewSort(){OpenCharacterSort();}','public void PreviewSort(){OpenCharacterSort();sortBegan=Now-1;}')
s=s.replace('public void PreviewHideCharacters(){dormOpen=sortOpen=roleOpen=roleClosing=false;}','public void PreviewHideCharacters(){dormOpen=sortOpen=roleOpen=roleClosing=homeIllustrationEditing=dormLeaving=pageMoving=sortClosing=false;}')
p.write_text(s,encoding='utf8')
print('Split dormant screen implementation; role-selection implementation retained.')
