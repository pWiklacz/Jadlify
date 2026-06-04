export interface DailyGoal {
  calories: number
  protein: number
  fat: number
  carbohydrates: number
}

export type UpsertDailyGoalRequest = DailyGoal

export type MealType = 'Breakfast' | 'Lunch' | 'Dinner' | 'Snack'

export const mealTypes: MealType[] = ['Breakfast', 'Lunch', 'Dinner', 'Snack']

export interface MealPlanEntry {
  id: string
  date: string
  recipeId: string
  recipeName: string
  mealType: MealType
  portions: number
}

export interface AddMealPlanEntryRequest {
  date: string
  recipeId: string
  mealType: MealType
  portions: number
}

export interface UpdateMealPlanEntryRequest {
  mealType: MealType
  portions: number
}

export interface CreatedMealPlanEntryResponse {
  id: string
}
