using Lumina.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inviter.Gui
{
    class Localizer
    {
        public string Language = "en";
        private Dictionary<string, string> zh = new Dictionary<string, string> { };
        private Dictionary<string, string> fr = new Dictionary<string, string> { };
        public Localizer(string language = "en")
        {
            Language = language;
            LoadZh();
            LoadFr();
        }
        public string Localize(string message)
        {
            if (message == null) return message;
            if (Language == "zh") return zh.ContainsKey(message) ? zh[message] : message;
            if (Language == "fr") return fr.ContainsKey(message) ? fr[message] : message;
            return message;
        }
        private void LoadZh()
        {
            zh.Add("Panel", "面板");
            zh.Add("TabBar", "标签栏");
            zh.Add("Settings", "设置");
            zh.Add("General Settings", "通用设置");
            zh.Add("Enable", "启用");
            zh.Add("Tooltips", "选项说明");
            zh.Add("Change the UI Language.", "更改UI语言");
            zh.Add("Eureka", "优雷卡");
            zh.Add("Delay(ms):", "延时(毫秒)：");
            zh.Add("Delay the invitation after triggered.", "触发后延迟邀请。");
            zh.Add("Language:", "语言：");
            zh.Add("Print Debug Message", "打印调试消息");
            zh.Add("Print Error Message", "打印错误消息");
            zh.Add("Filters", "过滤设置");
            zh.Add("Regex", "正则表达式");
            zh.Add("Filter out duplicates", "过滤重复消息");
            zh.Add("FiltersTable", "过滤表格");
            zh.Add("Debug", "调试");
            zh.Add("Urgent", "紧急");
            zh.Add("Notice", "通知");
            zh.Add("Say", "说话");
            zh.Add("Shout", "喊话");
            zh.Add("TellOutgoing", "发出悄悄话");
            zh.Add("TellIncoming", "收到悄悄话");
            zh.Add("Party", "小队");
            zh.Add("Alliance", "团队");
            zh.Add("FreeCompany", "部队");
            zh.Add("Ls1", "通讯贝1");
            zh.Add("Ls2", "通讯贝2");
            zh.Add("Ls3", "通讯贝3");
            zh.Add("Ls4", "通讯贝4");
            zh.Add("Ls5", "通讯贝5");
            zh.Add("Ls6", "通讯贝6");
            zh.Add("Ls7", "通讯贝7");
            zh.Add("Ls8", "通讯贝8");
            zh.Add("Yell", "呼喊");
            zh.Add("CrossParty", "跨服小队");
            zh.Add("PvPTeam", "PvP小队");
            zh.Add("NoviceNetwork", "新人频道");
            zh.Add("CrossLinkShell1", "跨服通讯贝1");
            zh.Add("CrossLinkShell2", "跨服通讯贝2");
            zh.Add("CrossLinkShell3", "跨服通讯贝3");
            zh.Add("CrossLinkShell4", "跨服通讯贝4");
            zh.Add("CrossLinkShell5", "跨服通讯贝5");
            zh.Add("CrossLinkShell6", "跨服通讯贝6");
            zh.Add("CrossLinkShell7", "跨服通讯贝7");
            zh.Add("CrossLinkShell8", "跨服通讯贝8");
            zh.Add("Echo", "默语");
            zh.Add("Automatically invite people to your party (doesn't work for CWLS).", "自动邀请人们参与组队（不适用于跨服通讯贝）。");
            zh.Add("Pattern of the chat message to trigger the invitation.", "触发邀请的聊天消息的模式串。");
            zh.Add("Use regex to match the pattern to chat messages.", "使用正则表达式匹配聊天消息。");
            zh.Add("Pattern:", "模式：");
            zh.Add("Sender", "发送者");
            zh.Add("Message", "消息");
            zh.Add("Time", "时间");
            zh.Add("Retrieve", "检索");
            zh.Add("Teleport", "传送");
            zh.Add("Delete", "删除");
            zh.Add("View", "查看");
            zh.Add("Tele", "传送");
            zh.Add("Del", "删除");
            zh.Add("Clear", "清空");
        }
        private void LoadFr()
        {
            fr.Add("Panel", "Config");
            fr.Add("TabBar", "TabBar");
            fr.Add("Settings", "Paramètres");
            fr.Add("General Settings", "Paramètres Généraux");
            fr.Add("Enable", "Activer");
            fr.Add("Tooltips", "Infobulle");
            fr.Add("Change the UI Language.", "Changer la langue de l'interface.");
            fr.Add("Eureka", "Eureka");
            fr.Add("Delay(ms):", "Délais (ms):");
            fr.Add("Delay the invitation after triggered.", "Delai de l'invitation après déclenchement.。");
            fr.Add("Language:", "Langage:");
            fr.Add("Print Debug Message", "Imprimer les messages de débuggage.");
            fr.Add("Print Error Message", "Imprimer les messages d'erreur.");
            fr.Add("Filters", "Filtres");
            fr.Add("Regex", "Regex");
            fr.Add("Filter out duplicates", "Filtrer les doublons");
            fr.Add("FiltersTable", "FiltersTable");
            fr.Add("Debug", "Débug");
            fr.Add("Urgent", "Urgent");
            fr.Add("Notice", "Avis");
            fr.Add("Say", "Dire");
            fr.Add("Shout", "Hurler");
            fr.Add("TellOutgoing", "Murmure Sortant");
            fr.Add("TellIncoming", "Murmure Entrant");
            fr.Add("Party", "Équipe");
            fr.Add("Alliance", "Alliance");
            fr.Add("FreeCompany", "Compagnie Libre");
            fr.Add("Ls1", "Ls1");
            fr.Add("Ls2", "Ls2");
            fr.Add("Ls3", "Ls3");
            fr.Add("Ls4", "Ls4");
            fr.Add("Ls5", "Ls5");
            fr.Add("Ls6", "Ls6");
            fr.Add("Ls7", "Ls7");
            fr.Add("Ls8", "Ls8");
            fr.Add("Yell", "Crier");
            fr.Add("CrossParty", "Inter-Monde");
            fr.Add("PvPTeam", "Équipe JcJ");
            fr.Add("NoviceNetwork", "Réseau des Novices");
            fr.Add("CrossLinkShell1", "Linkshell Inter-Monde1");
            fr.Add("CrossLinkShell2", "Linkshell Inter-Monde2");
            fr.Add("CrossLinkShell3", "Linkshell Inter-Monde3");
            fr.Add("CrossLinkShell4", "Linkshell Inter-Monde4");
            fr.Add("CrossLinkShell5", "Linkshell Inter-Monde5");
            fr.Add("CrossLinkShell6", "Linkshell Inter-Monde6");
            fr.Add("CrossLinkShell7", "Linkshell Inter-Monde7");
            fr.Add("CrossLinkShell8", "Linkshell Inter-Monde8");
            fr.Add("Echo", "Écho");
            fr.Add("Automatically invite people to your party (doesn't work for CWLS).", "Inviter automatiquement les personnes dans votre groupe (ne fonctionne pas pour les LS inter-mondes.");
            fr.Add("Pattern of the chat message to trigger the invitation.", "Pattern du message qui déclenche l'invitation.");
            fr.Add("Use regex to match the pattern to chat messages.", "Utiliser une regex pour identifier les messages.");
            fr.Add("Pattern:", "Pattern:");
            fr.Add("Sender", "Envoyeur");
            fr.Add("Message", "Message");
            fr.Add("Time", "Heure");
            fr.Add("Retrieve", "Récupérer");
            fr.Add("Teleport", "Téléporter");
            fr.Add("Delete", "Supprimer");
            fr.Add("View", "Inspected");
            fr.Add("Tele", "Télé");
            fr.Add("Del", "Del");
            fr.Add("Clear", "Nettoyer");
        }
    }
}
