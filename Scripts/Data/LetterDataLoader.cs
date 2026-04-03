using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

// [수정 포인트] List<JObject>를 상속받습니다.
// 이제 이 클래스는 그 자체로 '리스트' 취급을 받으므로, JSON 배열([ ... ])을 바로 받아들일 수 있습니다.
public class LetterDataLoader : List<JObject>, IDataLoader<string, LetterData>
{
    // 내부에 별도의 List 변수(public List<JObject> letters)를 선언할 필요가 없습니다.
    // 'this'가 곧 리스트입니다.

    public Dictionary<string, LetterData> MakeDict()
    {
        Dictionary<string, LetterData> dict = new Dictionary<string, LetterData>();

        // [수정 포인트] letters 대신 this(자기 자신)를 순회합니다.
        foreach (JObject jobj in this)
        {
            if (jobj.ContainsKey("type"))
            {
                string typeStr = jobj["type"].ToString();
                LetterData data = null;

                if (typeStr == "Apostle")
                    data = jobj.ToObject<ApostleLetterData>();
                else if (typeStr == "Visitor")
                    data = jobj.ToObject<VisitorLetterData>();

                if (data != null && !dict.ContainsKey(data.id))
                    dict.Add(data.id, data);
            }
        }
        return dict;
    }

    public bool Validate()
    {
        return true;
    }
}