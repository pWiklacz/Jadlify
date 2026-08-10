import type { ReactNode, SVGProps } from 'react'

/**
 * The mockups' line-icon set, extracted once so screens stop inlining raw SVG.
 *
 * Every glyph is a stroked path on a 20×20 grid that inherits `currentColor`, so
 * tone comes from a text-colour utility and size from the `size` prop. Icons are
 * decorative by default (`aria-hidden`): the control around them carries the
 * accessible name.
 */

interface IconProps extends Omit<SVGProps<SVGSVGElement>, 'children'> {
  /** Rendered width and height in px. */
  size?: number
}

function Icon({
  size = 16,
  viewBox = '0 0 20 20',
  strokeWidth = 1.8,
  children,
  ...rest
}: IconProps & { children: ReactNode }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox={viewBox}
      fill="none"
      stroke="currentColor"
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
      {...rest}
    >
      {children}
    </svg>
  )
}

/** Add / create. */
export function PlusIcon(props: IconProps) {
  return (
    <Icon strokeWidth={2.2} {...props}>
      <path d="M10 4v12M4 10h12" />
    </Icon>
  )
}

/** Forward affordance on primary CTAs. */
export function ArrowRightIcon(props: IconProps) {
  return (
    <Icon {...props}>
      <path d="M4 10h12M11 5l5 5-5 5" />
    </Icon>
  )
}

/** "Back to…" affordance. */
export function ChevronLeftIcon(props: IconProps) {
  return (
    <Icon strokeWidth={1.9} {...props}>
      <path d="M12.5 4 6.5 10l6 6" />
    </Icon>
  )
}

/** Disclosure affordance; rotate 180° with a transform when expanded. */
export function ChevronDownIcon(props: IconProps) {
  return (
    <Icon strokeWidth={1.9} {...props}>
      <path d="M5 8l5 5 5-5" />
    </Icon>
  )
}

/** Tick shown inside a checked box or a success banner. */
export function CheckIcon(props: IconProps) {
  return (
    <Icon strokeWidth={2.6} {...props}>
      <path d="M4 10.5l4 4L16 5.5" />
    </Icon>
  )
}

/** Search adornment inside the list filter field. */
export function SearchIcon(props: IconProps) {
  return (
    <Icon {...props}>
      <circle cx="9" cy="9" r="5.5" />
      <path d="m13.5 13.5 3.5 3.5" />
    </Icon>
  )
}

/** "No results" variant of the search glyph. */
export function SearchEmptyIcon(props: IconProps) {
  return (
    <Icon strokeWidth={1.6} {...props}>
      <circle cx="9" cy="9" r="5.5" />
      <path d="m13.5 13.5 3.5 3.5" />
      <path d="M7 9h4" />
    </Icon>
  )
}

/** Clear / dismiss. */
export function CloseIcon(props: IconProps) {
  return (
    <Icon strokeWidth={2} {...props}>
      <path d="M5 5l10 10M15 5 5 15" />
    </Icon>
  )
}

/** Caution triangle used by the plan-drift and incomplete-data banners. */
export function WarningIcon(props: IconProps) {
  return (
    <Icon strokeWidth={1.6} {...props}>
      <path d="M10 3 18 17H2z" />
      <path d="M10 8.5v3.5" />
      <circle cx="10" cy="14.4" r="0.5" fill="currentColor" />
    </Icon>
  )
}

/** Shopping trolley — the empty-state glyph for the list index. */
export function CartIcon(props: IconProps) {
  return (
    <Icon viewBox="0 0 24 24" strokeWidth={1.5} {...props}>
      <path d="M4 5h2l2.2 11.2a1.5 1.5 0 0 0 1.5 1.3h7.8a1.5 1.5 0 0 0 1.5-1.2L20.5 9H7" />
      <circle cx="10.5" cy="20.2" r="1.2" />
      <circle cx="17.5" cy="20.2" r="1.2" />
    </Icon>
  )
}
