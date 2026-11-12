using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameCore.Save;

/// <summary>
/// Grava e lê o arquivo de save (JSON) na pasta AppData.
/// No Windows: %AppData%\JogoLimbo\save.json
/// </summary>
public static class SaveManager
{
    // Nome da pasta do jogo dentro do AppData lembrar de trocar quando tiver nome.
    private const string GAME_FOLDER = "JogoLimbo";
    private const string SAVE_FILE = "save.json";

    private static readonly JsonSerializerOptions s_options = new()
    {
        // Deixa o arquivo legível (para abrir no Bloco de Notas e conferir).
        WriteIndented = true,

        // Grava enums como texto em vez de número.
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Pasta onde o save fica (criada automaticamente ao salvar).</summary>
    public static string SaveDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        GAME_FOLDER
    );

    /// <summary>Caminho completo do arquivo de save.</summary>
    public static string SavePath => Path.Combine(SaveDirectory, SAVE_FILE);

    /// <summary>Indica se existe um save gravado (para mostrar o botão "Continuar").</summary>
    public static bool HasSave() => File.Exists(SavePath);

    /// <summary>
    /// Grava o save. Retorna false se não foi possível gravar (disco cheio, sem
    /// permissão etc.), em vez de derrubar o jogo.
    /// </summary>
    public static bool Save(SaveData data)
    {
        try
        {
            Directory.CreateDirectory(SaveDirectory);

            data.Version = SaveData.CURRENT_VERSION;
            data.SavedAtUtc = DateTime.UtcNow;

            string json = JsonSerializer.Serialize(data, s_options);

            // Grava num arquivo temporário e só depois troca pelo definitivo.
            // Assim, se o jogo fechar no meio da gravação, o save anterior continua
            // intacto em vez de ficar um arquivo pela metade.
            string tempPath = SavePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, SavePath, overwrite: true);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Save] Falha ao salvar: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Lê o save. Retorna false se não existe, se está corrompido ou se foi criado
    /// por uma versão mais nova do jogo (o jogo continua normalmente).
    /// </summary>
    public static bool TryLoad(out SaveData data)
    {
        data = null;

        try
        {
            if (!File.Exists(SavePath))
            {
                return false;
            }

            string json = File.ReadAllText(SavePath);
            SaveData loaded = JsonSerializer.Deserialize<SaveData>(json, s_options);

            if (loaded == null || loaded.Version > SaveData.CURRENT_VERSION)
            {
                return false;
            }

            data = loaded;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Save] Falha ao carregar: {ex.Message}");
            data = null;
            return false;
        }
    }

    /// <summary>Apaga o save (por exemplo, ao começar um novo jogo).</summary>
    public static void Delete()
    {
        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Save] Falha ao apagar: {ex.Message}");
        }
    }
}