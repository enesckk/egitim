import type { Config } from 'tailwindcss';

export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          primary: '#17324D',
          accent: '#2A7F7B',
          secondary: '#4F6F8F',
        },
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
          900: '#17324D',
          800: '#1F4060',
          700: '#1E43A0',
          600: '#2451B7',
          500: '#3060CE',
          400: '#4F7BE3',
          100: '#D6E6FA',
          50: '#EBF2FD',
        },
        success: {
          DEFAULT: '#2F7D5A',
          dark: '#246347',
          light: '#ECFDF5',
        },
        warning: {
          DEFAULT: '#9A6A1A',
          dark: '#7D5413',
          light: '#FFFBEB',
        },
        danger: {
          DEFAULT: '#B44A4A',
          dark: '#923636',
          light: '#FEF2F2',
        },
        attention: {
          DEFAULT: '#7C3AED',
          dark: '#6D28D9',
          light: '#F5F3FF',
        },
        surface: {
          DEFAULT: '#F7F9FC',
          alt: '#EEF0F6',
          card: '#FFFFFF',
        },
        border: {
          subtle: '#D9E1E8',
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

