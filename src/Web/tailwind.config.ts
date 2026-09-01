import type { Config } from 'tailwindcss';

export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        navy: {
          950: '#060E1B',
          900: '#0F1B2D',
          800: '#192A42',
          700: '#243A5C',
          600: '#2F4872',
          500: '#3A5A8C',
          400: '#4F78B3',
          300: '#7099CC',
          200: '#A3BCE0',
          100: '#D3E2F2',
          50: '#EEF4FB',
        },
        primary: {
          900: '#192E80',
          700: '#1E43A0',
          600: '#2451B7',
          500: '#3060CE',
          400: '#4F7BE3',
          100: '#D6E6FA',
          50: '#EBF2FD',
        },
        success: {
          DEFAULT: '#059669',
          dark: '#047857',
          light: '#ECFDF5',
        },
        warning: {
          DEFAULT: '#D97706',
          dark: '#B45309',
          light: '#FFFBEB',
        },
        danger: {
          DEFAULT: '#DC2626',
          dark: '#B91C1C',
          light: '#FEF2F2',
        },
        attention: {
          DEFAULT: '#7C3AED',
          dark: '#6D28D9',
          light: '#F5F3FF',
        },
        surface: {
          DEFAULT: '#F7F8FB',
          alt: '#EEF0F6',
          card: '#FFFFFF',
        },
      },
      borderRadius: {
        sm: '6px',
        md: '8px',
        lg: '10px',
        xl: '12px',
        '2xl': '16px',
      },
      boxShadow: {
        'soft-sm': '0 1px 2px 0 rgba(15, 27, 45, 0.05)',
        'soft-md': '0 4px 6px -1px rgba(15, 27, 45, 0.06), 0 2px 4px -2px rgba(15, 27, 45, 0.04)',
        'soft-lg': '0 10px 15px -3px rgba(15, 27, 45, 0.06), 0 4px 6px -4px rgba(15, 27, 45, 0.03)',
      },
      fontFamily: {
        sans: ['DM Sans', 'system-ui', '-apple-system', 'sans-serif'],
        serif: ['DM Serif Display', 'Georgia', 'serif'],
        mono: ['JetBrains Mono', 'Courier New', 'monospace'],
      },
    },
  },
  plugins: [],
} satisfies Config;
