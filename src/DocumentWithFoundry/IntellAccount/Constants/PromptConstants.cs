namespace IntellAccount.Constants;

public class PromptConstants
{
    public static readonly string DefaultPrompt = @"Você atuará como um robô de inteligência capaz de fornecer respostas para o seu usuário, para perguntas " +
    "comuns, onde será importante fornecer resultados detalhados para um leigo nos assuntos";

    public static readonly string FirstSpeech = "Olá Márcio, o que você gostaria de perguntar ?";
    public static readonly string SecondarySpeech = "Você tem mais alguma pergunta sobre o contexto?";
    public static readonly string DocumentQuestion = "gostaria que você interpretasse um json, ele foi obtido a partir de um documento digitalizado, explique o que há neste documento conforme os dados recebidos, se forem produtos, informe alguns nomes e o valor total:";

    private static string InitialJsonPrompt = "gostaria que você interpretasse um json, ele foi obtido a partir de um " +
        "documento digitalizado, como dados de uma empresa, interprete as informações " +
        " e insira no modelo de dados fornecido a fim de realizar o input em um sistema computacional, fornecendo" +
        " como resposta apenas o json sem nenhum comentário ou explicação, segue o formato do Json a preencher: ";

    private static string FinalJsonPrompt = "não insira nenhuma tag na resposta fora o json puro! agora seguem os dados para interpretar e preencher:";

    public static string BusinessCard = InitialJsonPrompt +
        "{ {\r\n\"name\": \"\",\r\n\"street\" : \"\",\r\n\"number\" : \"\",\r\n\"neighborhood\" : \"\",\r\n\"city\" : \"\",\r\n\"state\" : \"\",\r\n\"zip\" : \"\",\r\n\"email\" : \"\",\r\n\"phone1\" : \"\",\r\n\"phone2\" : \"\"\r\n}} " +
        FinalJsonPrompt;

    public static string Receipt = InitialJsonPrompt +
        "{\r\n  \"eventdate\": \"\",  \"enterprise\": {\r\n        \"name\": \"\",\r\n        \"street\": \"\",\r\n        \"number\": \"\",\r\n        \"neighborhood\": \"\",\r\n        \"city\": \"\",\r\n        \"state\": \"\",\r\n        \"zip\": \"\",\r\n        \"email\": \"\",\r\n        \"phone1\": \"\",\r\n        \"phone2\": \"\"\r\n    },\r\n    \"products\": [\r\n        {\r\n            \"name\": \"\",\r\n            \"description\": \"\",\r\n            \"quantity\": 0,\r\n            \"unitPrice\": 0.0,\r\n            \"total\": 0.0\r\n        }\r\n    ],\r\n    \"total\": 0.0,\r\n    \"taxes\": 0.0\r\n}" +
        FinalJsonPrompt;

    public static string IdDocument = "gostaria que você interpretasse um json, ele foi obtido a partir de um " +
        "documento digitalizado, identifique o titular do documento, que normalmente estará nominado como titular ou com o campo nome" +
        ", se tiver dados da filiação (escrever o nome do pai e mãe), números de identificação, validade do documento, data de emissão," +
        "data de nascimento ";
}
