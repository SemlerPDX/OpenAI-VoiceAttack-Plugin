// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "ManualResetEventSlim instance is being reset after waiting, so it should not be marked as readonly -SemlerPDX Apr2023", Scope = "member", Target = "~F:OpenAI_VoiceAttack_Plugin.OpenAI_Plugin._asyncOperationCompleted")]
[assembly: SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "Personal Preference for forms design, one line per parameter including the form.Text -SemlerPDX Apr2023", Scope = "member", Target = "~M:OpenAI_VoiceAttack_Plugin.KeyForm.ShowKeyInputForm")]
[assembly: SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "vaProxy method parameters unknown to IDE prior to runtime", Scope = "member", Target = "~M:OpenAI_VoiceAttack_Plugin.OpenAI_Plugin.VA_Exit1(System.Object)")]
