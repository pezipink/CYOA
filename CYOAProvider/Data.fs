namespace CYOAProvider.Data

type Key = string

/// Key is a page number of some kind of index, text is the core page text,
/// routes are the options available to the player along with the page key they navigate to
type PageEntry = { Key: Key; Text : string; Choices : (string * Key) list }