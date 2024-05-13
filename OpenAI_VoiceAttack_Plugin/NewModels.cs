using OpenAI_API;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAI_VoiceAttack_Plugin
{
    public class NewModels : Model
    {
        public static Model TextModerationStable => new Model("text-moderation-stable") { OwnedBy = "openai" };
    }
}
