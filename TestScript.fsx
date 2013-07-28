#r @"path\CYOAProvider.dll "
type adventure = CYOAProvider.CYOAProvider< @"F:\dropbox\CYOAProvider\data.dat">
let a = adventure()

a.Intro  // start your adventure here!




