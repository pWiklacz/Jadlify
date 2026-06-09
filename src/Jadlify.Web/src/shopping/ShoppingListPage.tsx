import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useShoppingList } from './useShoppingList'

export function ShoppingListPage() {
  const [date, setDate] = useState(todayIsoDate())
  const { data: shoppingList, isLoading, isError } = useShoppingList(date)

  return (
    <section className="flex flex-col gap-5">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-bold">Shopping list</h1>
        <p className="max-w-2xl text-sm text-slate-600">
          Generated from the recipes planned for one day.
        </p>
      </div>

      <label className="flex max-w-xs flex-col gap-1 text-sm font-medium text-slate-700">
        Date
        <input
          type="date"
          value={date}
          onChange={(event) => setDate(event.currentTarget.value)}
          className="rounded-md border border-slate-300 px-3 py-2 text-base font-normal text-slate-900"
        />
      </label>

      {isLoading && <p className="text-slate-600">Loading shopping list.</p>}

      {isError && (
        <p role="alert" className="text-red-600">
          Could not load the shopping list. Please refresh to try again.
        </p>
      )}

      {shoppingList && shoppingList.warnings.length > 0 && (
        <div
          role="alert"
          className="flex flex-col gap-2 rounded-md border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900"
        >
          <p className="font-medium">This list may be incomplete.</p>
          <ul className="list-disc space-y-1 pl-5">
            {shoppingList.warnings.map((warning) => (
              <li key={`${warning.entryId}-${warning.recipeId}`}>{warning.message}</li>
            ))}
          </ul>
        </div>
      )}

      {shoppingList && shoppingList.items.length === 0 && (
        <div className="rounded-md border border-slate-200 bg-white p-4 text-sm text-slate-600">
          <p>No planned ingredients for this date.</p>
          <Link to="/meal-plan" className="mt-2 inline-flex font-medium text-slate-900 underline">
            Plan recipes
          </Link>
        </div>
      )}

      {shoppingList && shoppingList.items.length > 0 && (
        <div className="flex flex-col gap-3">
          <h2 className="text-lg font-semibold">Items for {shoppingList.date}</h2>
          <ul className="divide-y divide-slate-200 overflow-hidden rounded-lg border border-slate-200 bg-white">
            {shoppingList.items.map((item) => (
              <li
                key={item.productId}
                className="flex flex-col gap-1 px-4 py-3 sm:flex-row sm:items-center sm:justify-between"
              >
                <span className="break-words font-medium text-slate-900">{item.productName}</span>
                <span className="text-sm font-semibold text-slate-700">
                  {formatGrams(item.grams)} g
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  )
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

function formatGrams(grams: number): string {
  return grams.toFixed(1)
}
