using System;
using System.Collections.Generic;
using YoungManGomoku_Protocol.Source.TypeEnum;

// Protocol의 PlayerData는 단순 서버와 통신용
// 서버 DB 테이블과의 구조 역시 다르다
// 클라이언트 빌드에 포함되기 때문에 보안상 중요한 코드는 Protocol에 올리지 말 것

namespace YoungManGomoku_Protocol.Source
{
    public class CS_GoogleAccountRegisterDTO
    {
        public string UserNickname { get; set; }
        public string IdToken { get; set; }        
    }

}
