# recipe-conv

A small program for converting exported recipe data from [Mela](https://apps.apple.com/us/app/mela-recipe-manager/id1548466041)
to [Paprika](https://www.paprikaapp.com/).

My original use case was wanting to try [Crouton](https://apps.apple.com/us/app/crouton-cooking-companion/id1461650987) on iOS but all
my recipe data was stuck in Mela.

Mela offers a bulk recipe export feature, but can only export data in its own `.melarecipes` format.
Crouton offers a bulk import feature, but can only import data from its own `.zip` export _or_ from Paprika's `.paprikarecipes` export.

Crouton's recipe representation is fairly complicated and requires being able to identify and parse units of measure.
To avoid taking on this complexity (read: out of sheer laziness) I instead implemented a Mela -> Paprika conversion tool and relied on Crouton's "import from Paprika" feature.

Note: Crouton unfortunately does not explode instructions into steps when importing a Paprika archive.

# File formats

## Paprika

`.paprikarecipes` is a zip archive containing `.paprikarecipe` files.
Each recipe file is a gzip-compressed JSON document.

## Mela

`.melarecipes` is a zip archive containing `.melarecipe` files.
Each recipe file is a JSON document, no compression.

## Crouton

`.zip` is a zip archive containing `.crumb` files.
Each recipe file a JSON document, no compression.
