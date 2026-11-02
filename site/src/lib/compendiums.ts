/** A game compendium, with the logo that compendiums.org shows for it. */
export interface Compendium {
	name: string;
	url: string;
	icon: string;
}

/** The game compendiums, in the order compendiums.org lists them. */
export const compendiums: readonly Compendium[] = [
	{ name: 'Afallon', url: 'https://afallon.compendiums.org/', icon: 'afallon' },
	{
		name: 'Ancient Kingdoms',
		url: 'https://ancient-kingdoms.compendiums.org/',
		icon: 'ancient-kingdoms'
	},
	{ name: 'Erenshor', url: 'https://erenshor.compendiums.org/', icon: 'erenshor' },
	{ name: 'The Bloodline', url: 'https://the-bloodline.compendiums.org/', icon: 'the-bloodline' }
];

/** This compendium's entry in the list. */
export const thisCompendium = 'the-bloodline';

/** The compendiums for the other games. */
export const otherCompendiums = compendiums.filter(({ icon }) => icon !== thisCompendium);

export const iconUrl = (compendium: Compendium) =>
	`https://compendiums.org/icons/${compendium.icon}.webp`;
