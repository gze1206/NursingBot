using Discord;
using Discord.Interactions;
using NursingBot.Feature.Preconditions;
using NRandom = NursingBot.Core.Random;

namespace NursingBot.Feature;

public class RandomModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly string[] YesNo = new[]
    {
        "YES", "NO"
    };

    private static readonly string[] Magic8Ball = new[]
    {
        "확실합니다.", "확실하게 그렇습니다.", "의심의 여지가 없네요.", "확실히 예라고 답하겠습니다.", "믿으셔도 됩니다.",
        "제가 보기엔 그렇습니다.", "가장 가능성이 높네요.", "전망이 좋습니다.", "예.", "애매하네요.", "나중에 다시 물어보세요.",
        "지금은 예측할 수 없네요.", "질문을 조금 더 다듬어주세요.", "기대하지 마세요.", "아니오.", "제가 듣기론 아니네요.", "전망이 그리 좋지 않아요.",
        "심히 의심됩니다."
    };

    private static readonly char pickDelimiter = ',';

    [SlashCommand("yn", "질문에 대해 Yes or No로 답변합니다.")]
    [RequireRegister]
    public Task YesOrNoAsync([Summary("question", "답변을 듣고 싶은 질문을 입력해주세요.")] string question)
    {
        var embed = new EmbedBuilder()
            .WithTitle("질문에 대한 답입니다.")
            .AddField("Question", question)
            .AddField("Answer", YesNo[NRandom.Next(2)])
            .Build();

        return this.Context.Interaction.RespondAsync(embed: embed);
    }

    [SlashCommand("m8b", "질문에 대해 임의로 답변합니다.")]
    [RequireRegister]
    public Task Magic8BallAsync([Summary("question", "답변을 듣고 싶은 질문을 입력해주세요.")] string question)
    {
        var embed = new EmbedBuilder()
            .WithTitle("질문에 대한 답입니다.")
            .AddField("Question", question)
            .AddField("Answer", Magic8Ball[NRandom.Next(Magic8Ball.Length)])
            .Build();

        return this.Context.Interaction.RespondAsync(embed: embed);
    }

    [SlashCommand("pick", "질문에 대해 임의로 답변합니다.\n답변은 질문과 함께 입력한 답변 목록에서 하나를 선택합니다.\n답변 목록이 비었거나 항목이 2개 미만일 경우 Yes or No로 답변합니다.")]
    [RequireRegister]
    public Task PickAsync(
        [Summary("question", "답변을 듣고 싶은 질문을 입력해주세요.")]
        string question,
        [Summary("answers", "답변 목록을 작성합니다.\n각 답변은 ,로 구분해 입력합니다.\n답변 목록을 생략하면 Yes or No로 답변됩니다.\n예시 - 답1,답2,답3")]
        string rawAnswers
    )
    {
        var answers = rawAnswers.Split(pickDelimiter).ToArray();

        if (answers.Length < YesNo.Length)
        {
            answers = YesNo;
        }

        var embed = new EmbedBuilder()
            .WithTitle("질문에 대한 답입니다.")
            .AddField("Question", question)
            .AddField("Answer", answers[NRandom.Next(answers.Length)])
            .Build();

        return this.Context.Interaction.RespondAsync(embed: embed);
    }
}