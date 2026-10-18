using PetFlowAPI.Extensions;

namespace PetFlowAPI.Tests.Integration;

public class DotEnvLoaderTests
{
    [Fact]
    public void Parse_DeveIgnorarComentariosEBrancosEManterIgualDentroDoValor()
    {
        // Arrange
        var lines = new[]
        {
            "# comentário", "", "  ", "Jwt__Key=abc==", "export Mongo__Db = petflow", "SEM_IGUAL", "=semchave"
        };

        // Act
        var result = DotEnvLoader.Parse(lines).ToDictionary(p => p.Key, p => p.Value);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("abc==", result["Jwt__Key"]);
        Assert.Equal("petflow", result["Mongo__Db"]);
    }

    [Theory]
    [InlineData("A=\"valor com espaço\"", "valor com espaço")]
    [InlineData("A='aspas simples'", "aspas simples")]
    [InlineData("A=Data Source=(X=1);User Id=u;", "Data Source=(X=1);User Id=u;")]
    public void Parse_DeveRemoverAspasEPreservarConnectionStrings(string line, string expected)
    {
        // Arrange & Act
        var value = DotEnvLoader.Parse(new[] { line }).Single().Value;

        // Assert
        Assert.Equal(expected, value);
    }

    [Fact]
    public void Load_NaoDeveSobrescreverVariavelJaDefinidaNoAmbiente()
    {
        // Arrange
        var existing = $"PETFLOW_TEST_{Guid.NewGuid():N}";
        var fresh = $"PETFLOW_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(existing, "do-sistema");
        var file = Path.GetTempFileName();
        File.WriteAllLines(file, new[] { $"{existing}=do-arquivo", $"{fresh}=novo" });

        try
        {
            // Act
            DotEnvLoader.Load(file);

            // Assert
            Assert.Equal("do-sistema", Environment.GetEnvironmentVariable(existing));
            Assert.Equal("novo", Environment.GetEnvironmentVariable(fresh));
        }
        finally
        {
            File.Delete(file);
            Environment.SetEnvironmentVariable(existing, null);
            Environment.SetEnvironmentVariable(fresh, null);
        }
    }

    [Fact]
    public void FindFile_DeveSubirDiretoriosAteEncontrarOEnv()
    {
        // Arrange
        var root = Directory.CreateTempSubdirectory().FullName;
        var nested = Directory.CreateDirectory(Path.Combine(root, "a", "b")).FullName;
        File.WriteAllText(Path.Combine(root, ".env"), "X=1");

        try
        {
            // Act
            var found = DotEnvLoader.FindFile(nested);

            // Assert
            Assert.Equal(Path.Combine(root, ".env"), found);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
