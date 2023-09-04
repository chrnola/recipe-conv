module Mela =
    type RecipeHeader =
        { oridnal: int
          title: string }
    with
        static member ofFilename (s: string): RecipeHeader =
            let fail() =
                failwithf "Could not parse Mela recipe filename: %s" s

            let dashOrdinal =
                match s.IndexOf('-') with
                | -1 | 0 ->
                    fail()
                | idx -> idx

            let title =
                match s.LastIndexOf(".melarecipe") with
                | -1 | 0 ->
                    fail()
                | idx ->
                    s.Substring(dashOrdinal + 1, (idx - dashOrdinal - 1))

            { oridnal = s.Substring(0, dashOrdinal) |> System.Convert.ToInt32
              title = title }

    [<Literal>]
    let private EpochOffset = 978307200L

    type RecipePayload =
        { /// Unique ID, e.g. "01FF07B7-2F71-4FEE-A01C-9FAFD36237DE-11638-00003DAC76F5F77E"
          id: string
          /// List of categories this recipe belongs to
          categories: string[]
          /// Newline-delimited list of nutrition facts, e.g. "Trans Fat: 0 grams\nFat: 36 grams\nCalories: 508..."
          nutrition: string
          /// Flag indicating whether user has marked as favorite
          favorite: bool
          /// The final output of this recipe e.g. "Yield 4 servings"
          ``yield``: string
          /// Human-readable duration of time required to cook this recipe e.g. "40 minutes"
          cookTime: string
          /// URL (slashes escaped) pointing to the source of this recipe
          link: string
          /// Human-readable duration of time required to complete this recipe e.g. "40 minutes"
          totalTime: string
          /// Name of the recipe
          title: string
          /// Probably newline-delimited notes field.
          notes: string
          /// Seconds since midnight UTC on January 1 2001 (add 978307200 to get epoch time)
          /// e.g. 711149256.442811 represents Saturday, July 15, 2023 5:27:36 PM GMT-04:00
          date: decimal
          /// Newline delimited list
          ingredients: string
          /// Not sure
          text: string
          /// Human-readable duration of time required to prep this recipe e.g. "40 minutes"
          prepTime: string
          /// List of base-64 encoded images
          images: string[]
          /// Flagged for future cooking
          wantToCook: bool
          /// Newline-delimited list of cooking instructions
          instructions: string }

    let parseDate (date: decimal): System.DateTimeOffset =
        let seconds = date |> System.Math.Floor
        let fractional = (date - seconds) * 1000m
        let epochMilliseconds = (int64 (seconds) * 1000L) + EpochOffset
        System.DateTimeOffset.FromUnixTimeMilliseconds (epochMilliseconds + (int64 fractional))

    let listRecipes file = seq {
        use zip = System.IO.Compression.ZipFile.OpenRead file

        for x in zip.Entries do
            use stream = x.Open()
            let recipe = System.Text.Json.JsonSerializer.Deserialize<RecipePayload> stream

            yield RecipeHeader.ofFilename x.Name, recipe
    }

module Paprika =
    [<Literal>]
    let private RecipeFileExtension = ".paprikarecipe"

    type RecipePayload =
        { /// Identifier
          uid: string
          /// e.g. "", "Easy", "Medium", "Hard"
          difficulty: string
          /// Similar to yield
          servings: string
          /// Overview of recipe
          description: string
          /// SHA-256 of...something
          hash: string
          /// Base-64 encoded image data
          photo_data: string
          /// Filename pointing to an object in the `photos` array
          photo_large: string
          /// Modifications and personal notes about the recipe
          notes: string
          /// Filename of an image
          photo: string
          /// Duartion of time
          cook_time: string
          /// The URL from which the `photo_data` blob was fetched
          image_url: string
          /// A collection of additional photos associated with this recipe.
          photos: Photo[]
          /// Title of the recipe
          name: string
          /// Total Duartion of time
          total_time: string
          /// Names of all the categories the user has added this recipe to.
          categories: string[]
          /// Newline-delimited list of nutrition facts
          nutritional_info: string
          /// Assembly instruction for this recipe
          directions: string
          /// A datetime string indiciating when this recipe was created e.g. "2020-11-24 19:56:43"
          created: string
          /// URL pointing to the source of this recipe
          source_url: string
          /// User-assigned rating, probably 0-5 stars (e.g. 0)
          rating: int
          /// Root domain name of `source_url`, e.g. "smittenkitchen.com"
          source: string
          /// Newline-delimited list of ingredients
          ingredients: string
          /// Duartion of time
          prep_time: string
          /// SHA-256 of the base-64 decoded Hash of `photo_data`?
          photo_hash: string }
    and Photo =
        { name: string
          data: string
          filename: string
          hash: string }

    let toDateString (dt: System.DateTimeOffset): string = dt.LocalDateTime.ToString("yyyy-MM-mm HH:mm:ss")

    let listRecipes file = seq {
        use zip = System.IO.Compression.ZipFile.OpenRead file

        for x in zip.Entries do
            use stream = x.Open()
            use decompressionStream =
                new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress)

            yield System.Text.Json.JsonSerializer.Deserialize<RecipePayload> decompressionStream
    }

    let writeExportArchive (outputFile: string) (recipes: RecipePayload seq) =
        use zip = System.IO.Compression.ZipFile.Open(outputFile, System.IO.Compression.ZipArchiveMode.Create)

        for recp in recipes do
            let entry = zip.CreateEntry($"{recp.name}{RecipeFileExtension}")
            use stream = entry.Open()
            use compressionStream = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Compress)
            do System.Text.Json.JsonSerializer.Serialize(compressionStream, recp)

module Hash =
    let sha256: byte[] -> string = System.Security.Cryptography.SHA256.HashData >> System.Convert.ToHexString

module Conversion =
    let melaToPaprika (_: Mela.RecipeHeader, mr: Mela.RecipePayload): Paprika.RecipePayload =
        let img = Array.tryHead mr.images
        { uid = System.Guid.NewGuid().ToString()
          difficulty = ""
          servings = mr.``yield``
          description = mr.text
          hash = mr.id |> System.Text.Encoding.UTF8.GetBytes |> Hash.sha256
          photo_data = img |> Option.defaultValue null
          photo_large = null
          notes = mr.notes
          photo = null
          cook_time = mr.cookTime
          image_url = ""
          photos = [||]
          name = mr.title
          total_time = mr.totalTime
          categories = mr.categories
          nutritional_info = mr.nutrition
          directions = mr.instructions
          created = mr.date |> Mela.parseDate |> Paprika.toDateString
          source_url = mr.link
          rating = if mr.favorite then 5 else 0
          source =
            try
                let uri = System.Uri(mr.link)
                match uri.HostNameType with
                | System.UriHostNameType.Basic
                | System.UriHostNameType.Dns -> uri.Host
                | _ -> null
            with
                | _ -> null
          ingredients = mr.ingredients
          prep_time = mr.prepTime
          photo_hash =
            img
            |> Option.map (System.Convert.FromBase64String >> Hash.sha256)
            |> Option.defaultValue null }

Mela.listRecipes "samples/mela/MelaExport.melarecipes"
|> Seq.map (Conversion.melaToPaprika)
|> Paprika.writeExportArchive "out/pap.paprikarecipes"
