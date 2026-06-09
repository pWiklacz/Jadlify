export interface ShoppingList {
  date: string
  items: ShoppingListItem[]
  warnings: ShoppingListWarning[]
}

export interface ShoppingListItem {
  productId: string
  productName: string
  grams: number
}

export interface ShoppingListWarning {
  entryId: string
  recipeId: string
  message: string
}
