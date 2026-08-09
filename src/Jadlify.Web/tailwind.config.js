/** @type {import('tailwindcss').Config} */

/*
 * Jadlify design tokens (single source of truth for the redesign).
 *
 * Palette, fonts, radii, shadows and animations are extracted from the mockups
 * in `docs/design/`. Components MUST consume these named tokens rather than
 * copying raw hex values out of the mockups (see the plan's Phase 1 contract).
 *
 * Two surfaces: a warm dark shell (`ink` background, `parchment` text) and cream
 * cards (`cream` surfaces, `espresso` text). Text opacity on the dark shell is
 * expressed with Tailwind opacity modifiers, e.g. `text-parchment/55`.
 */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        // Dark shell.
        ink: {
          DEFAULT: '#1F1712',
          raised: '#2C2015',
          800: '#2A2018',
          700: '#3A2C1F',
        },
        // Primary text on the dark shell.
        parchment: '#F2E9D8',
        // Text / icon color on filled terracotta and danger actions.
        paper: '#FFF8EC',
        // Warm-tinted modal/backdrop scrim.
        overlay: 'rgba(18,12,8,0.6)',
        // Cream card surfaces.
        cream: {
          DEFAULT: '#F8F1E3',
          panel: '#FCF7EC',
          input: '#FFFDF8',
          hover: '#EFE5D2',
          track: '#EBE0CB',
          line: '#DCCFB2',
          border: '#EADFC8',
          // Resting outline of a tickable box on cream (checkbox, day cell mark).
          mark: '#C9BB9E',
        },
        // Text on cream.
        espresso: '#2A2018',
        mocha: '#8C7F6C',
        label: '#5C5040',
        // De-emphasised text on cream: a ticked-off product line.
        muted: '#A79A83',
        // Tertiary text on cream: counts, leader lines, footnotes.
        faint: '#B0A48C',
        // Terracotta accent / primary action.
        terracotta: {
          DEFAULT: '#D97E57',
          strong: '#C75B38',
          hover: '#B04E2E',
        },
        // Feedback tones.
        danger: {
          DEFAULT: '#B3402E',
          ink: '#8F3222',
        },
        success: {
          DEFAULT: '#5F7C4E',
          ink: '#4C6340',
          dot: '#8FBF6A',
          onDark: '#AFCB90',
        },
        warning: {
          DEFAULT: '#C9A227',
          ink: '#8A6414',
          onDark: '#D9B25E',
        },
        // Per-macro colors (bars / swatches).
        macro: {
          protein: '#C75B38',
          carbs: '#C9A227',
          fat: '#6D8B5C',
        },
      },
      fontFamily: {
        sans: ['Archivo', 'system-ui', 'sans-serif'],
        serif: ['"Instrument Serif"', 'Georgia', 'serif'],
      },
      borderRadius: {
        pill: '999px',
        card: '20px',
        panel: '14px',
        field: '12px',
      },
      boxShadow: {
        card: '0 20px 50px rgba(0,0,0,0.25)',
        modal: '0 30px 80px rgba(0,0,0,0.5)',
        toast: '0 12px 32px rgba(0,0,0,0.4)',
        fab: '0 14px 34px rgba(0,0,0,0.45)',
        authcard: '0 30px 70px -30px rgba(0,0,0,0.55)',
      },
      backgroundImage: {
        // Warm radial glow behind the dark shell.
        'ink-radial':
          'radial-gradient(1100px 500px at 50% -120px, #2C2015 0%, #1F1712 70%)',
      },
      screens: {
        // The redesign's desktop/mobile boundary (mobile ≤ 940px). Base utility
        // = mobile; the `design:` prefix targets desktop (min-width: 940px).
        design: '940px',
      },
      letterSpacing: {
        eyebrow: '0.16em',
        tagline: '0.26em',
      },
      keyframes: {
        'jd-spin': {
          to: { transform: 'rotate(360deg)' },
        },
        'jd-pulse': {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.45' },
        },
        'jd-rise': {
          from: { opacity: '0', transform: 'translateY(8px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        'jd-toast': {
          from: { opacity: '0', transform: 'translateY(20px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        'jd-fade': {
          from: { opacity: '0' },
          to: { opacity: '1' },
        },
      },
      animation: {
        'spin-slow': 'jd-spin 0.8s linear infinite',
        'pulse-soft': 'jd-pulse 1.4s ease-in-out infinite',
        rise: 'jd-rise 0.4s ease-out',
        toast: 'jd-toast 0.28s ease-out',
        fade: 'jd-fade 0.3s ease-out',
      },
    },
  },
  plugins: [],
}
