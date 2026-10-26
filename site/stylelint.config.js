/**
 * Raw values live only in src/tokens.css. Every other style picks a token, so spacing, type, radii,
 * colours and layers stay on the same small scales and a new page cannot introduce a near-miss value.
 */
// A spacing value is a list of 0, auto, spacing tokens or negated spacing tokens.
const spacing =
	'/^(\\s*(0|auto|var\\(--space-[a-z0-9-]+\\)|calc\\(-1 \\* var\\(--space-[a-z0-9-]+\\)\\)))+\\s*$/';
const token = (scale) => `/^var\\(--${scale}-[a-z0-9-]+\\)$/`;

/** @type {import('stylelint').Config} */
export default {
	overrides: [
		{ files: ['**/*.svelte'], customSyntax: 'postcss-html' },
		{
			files: ['src/tokens.css'],
			rules: {
				'color-no-hex': null,
				'function-disallowed-list': null,
				'declaration-property-unit-allowed-list': null,
				'property-disallowed-list': null,
				'declaration-property-value-allowed-list': null
			}
		}
	],
	rules: {
		'color-no-hex': true,
		'color-named': 'never',
		'function-disallowed-list': [
			'rgb',
			'rgba',
			'hsl',
			'hsla',
			'hwb',
			'lab',
			'lch',
			'oklab',
			'oklch',
			'color-mix'
		],
		// Declarations take lengths from tokens. Media queries cannot read tokens, so they may use rem,
		// but only at the breakpoints allowed below.
		'unit-allowed-list': ['%', 'fr', 'deg', 'ms', 's', 'rem', 'px', 'em', 'ch', 'vw', 'vh', 'dvh'],
		'declaration-property-unit-allowed-list': { '/.*/': ['%', 'fr', 'deg', 'ms', 's'] },
		'media-feature-name-value-allowed-list': { width: ['40rem', '64rem'] },
		// Components may only map Pagefind's own variables, and only to tokens.
		'property-disallowed-list': ['/^--(?!pf-)/', 'letter-spacing'],
		'declaration-property-value-disallowed-list': { 'text-transform': ['uppercase'] },
		'declaration-property-value-allowed-list': {
			'/^(margin|padding|gap|row-gap|column-gap|inset|top|right|bottom|left|scroll-margin)/': [
				spacing,
				token('position')
			],
			'font-size': [token('type'), 'inherit'],
			'line-height': [token('leading'), 'inherit'],
			'font-weight': [token('weight'), 'inherit'],
			'font-family': [token('font'), 'inherit'],
			'/^border(-[a-z]+)*-radius$/': ['0', token('radius')],
			'z-index': [token('layer')],
			'/^--pf-/': ['/^var\\(--[a-z0-9-]+\\)$/', 'none', 'transparent', '100%']
		}
	},
	ignoreFiles: ['build/**', '.svelte-kit/**', 'node_modules/**', 'test-results/**']
};
