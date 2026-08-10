import { forwardRef } from 'react'
import type { InputHTMLAttributes, ReactNode } from 'react'

type LabelVariant = 'eyebrow' | 'plain'

const LABEL_CLASSES: Record<LabelVariant, string> = {
  eyebrow: 'text-[11px] font-bold uppercase tracking-eyebrow text-label',
  plain: 'text-[13px] font-semibold text-label',
}

interface FieldProps {
  /** Id of the control this field labels; also namespaces the error node. */
  id: string
  label: ReactNode
  /** Error text; when present the field is styled invalid and announced. */
  error?: string | null
  /** Appends a muted "(OPCJONALNIE)" marker to the label. */
  optional?: boolean
  /** Supporting help text rendered under the label. */
  hint?: ReactNode
  labelVariant?: LabelVariant
  children: ReactNode
}

/**
 * Layout wrapper for a labelled form control on a cream surface: an uppercase
 * eyebrow label (or a plain label), optional hint, the control, and an alert
 * error line. Callers wire `aria-invalid`/`aria-describedby` on the control
 * using the derived id conventions (`{id}-error`, `{id}-hint`), or use the
 * higher-level {@link TextField} which does it for them.
 */
export function Field({
  id,
  label,
  error,
  optional = false,
  hint,
  labelVariant = 'eyebrow',
  children,
}: FieldProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className={LABEL_CLASSES[labelVariant]}>
        {label}
        {optional && <span className="ml-1 font-medium text-mocha"> (opcjonalnie)</span>}
      </label>
      {hint && (
        <p id={`${id}-hint`} className="text-[13px] text-mocha">
          {hint}
        </p>
      )}
      {children}
      {error && (
        <p
          id={`${id}-error`}
          role="alert"
          className="flex items-center gap-1.5 text-[12.5px] text-danger"
        >
          <span aria-hidden="true">▲</span>
          {error}
        </p>
      )}
    </div>
  )
}

interface TextInputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean
}

/** Styled text input for cream surfaces. Border turns red when `invalid`. */
export const TextInput = forwardRef<HTMLInputElement, TextInputProps>(
  function TextInput({ invalid = false, className = '', ...rest }, ref) {
    return (
      <input
        ref={ref}
        aria-invalid={invalid || undefined}
        className={[
          'w-full rounded-field border-[1.5px] bg-cream-input px-3.5 py-3 text-espresso outline-none transition-colors placeholder:text-mocha/70 focus:border-terracotta disabled:opacity-60',
          invalid ? 'border-danger' : 'border-cream-border',
          className,
        ]
          .filter(Boolean)
          .join(' ')}
        {...rest}
      />
    )
  },
)

interface TextFieldProps extends Omit<TextInputProps, 'id'> {
  id: string
  label: ReactNode
  error?: string | null
  optional?: boolean
  hint?: ReactNode
  labelVariant?: LabelVariant
  /** Extra element rendered inside the input row (e.g. a show/hide toggle). */
  trailing?: ReactNode
}

/**
 * A fully wired labelled text input: composes {@link Field} and
 * {@link TextInput}, links the label, and points `aria-describedby` at the
 * hint/error nodes so assistive tech announces them.
 */
export const TextField = forwardRef<HTMLInputElement, TextFieldProps>(
  function TextField(
    { id, label, error, optional, hint, labelVariant, trailing, ...inputProps },
    ref,
  ) {
    const describedBy =
      [hint ? `${id}-hint` : null, error ? `${id}-error` : null]
        .filter(Boolean)
        .join(' ') || undefined

    return (
      <Field
        id={id}
        label={label}
        error={error}
        optional={optional}
        hint={hint}
        labelVariant={labelVariant}
      >
        <div className="relative">
          <TextInput
            ref={ref}
            id={id}
            invalid={Boolean(error)}
            aria-describedby={describedBy}
            className={trailing ? 'pr-14' : ''}
            {...inputProps}
          />
          {trailing && (
            <div className="absolute inset-y-0 right-1.5 flex items-center">
              {trailing}
            </div>
          )}
        </div>
      </Field>
    )
  },
)
