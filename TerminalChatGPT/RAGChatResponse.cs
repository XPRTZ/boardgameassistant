using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TalkToYourData;
public record RAGChatResponse(Guid Id, string Question, string Answer, string Context);
