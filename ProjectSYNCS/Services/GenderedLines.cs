using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// A Plynling pool in both genders. Every line about one specific Plynling lives in one of
// these, so a call site cannot pick a line without saying whose — `.For(p.Gender)` — and a
// female Plynling can never be handed a masculine line through a forgotten switch.
//
// The halves are ordinary arrays, so ResponsePicker is unchanged. A half may repeat a line
// from the other when there is nothing in it to agree ("**{0}** frétille de bonheur ✨").
public sealed record GenderedLines(string[] M, string[] F)
{
    public string[] For(PlynlingGender gender) => gender == PlynlingGender.Female ? F : M;
}
