using UnityEngine;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;

[CreateAssetMenu(fileName = "ProfileImages", menuName = "Scriptable Objects/ProfileImages")]
public class ProfileImages : ScriptableObject
{
     [SerializeField] private Sprite none;
     [SerializeField] private Sprite studentBoy;
     [SerializeField] private Sprite studentGirl;
     [SerializeField] private Sprite gentleMan;
     [SerializeField] private Sprite lady;
     [SerializeField] private Sprite grandFather;
     [SerializeField] private Sprite grandMather;

     private void OnEnable() => Debug.Assert((int)ProfileImageType.MAXCOUNT == 7,
         "패킷 프로토콜에서 정의한 이미지 개수와 일치하지 않음");
     
     public Sprite this[ProfileImageType type]
         => type switch
         {
             ProfileImageType.StudentBoy => studentBoy,
             ProfileImageType.StudentGirl => studentGirl,
             ProfileImageType.GentleMan => gentleMan,
             ProfileImageType.Lady => lady,
             ProfileImageType.GrandFather => grandFather,
             ProfileImageType.GrandMather => grandMather,
             _ => none
         };
}
