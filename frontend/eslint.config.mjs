import nx from '@nx/eslint-plugin';

export default [
  ...nx.configs['flat/base'],
  ...nx.configs['flat/typescript'],
  ...nx.configs['flat/javascript'],
  {
    ignores: ['**/dist', '**/generated/**', '**/test-results/**', '**/playwright-report/**'],
  },
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.js', '**/*.jsx'],
    rules: {
      '@nx/enforce-module-boundaries': [
        'error',
        {
          enforceBuildableLibDependency: true,
          allow: ['^.*/eslint(\\.base)?\\.config\\.[cm]?[jt]s$'],
          depConstraints: [
            {
              sourceTag: 'type:app',
              onlyDependOnLibsWithTags: ['type:feature', 'type:config', 'type:proxy', 'type:ui', 'type:core'],
            },
            {
              sourceTag: 'type:feature',
              onlyDependOnLibsWithTags: ['type:proxy', 'type:ui', 'type:core'],
            },
            { sourceTag: 'type:config', onlyDependOnLibsWithTags: ['type:core'] },
            { sourceTag: 'type:proxy', onlyDependOnLibsWithTags: ['type:core'] },
            { sourceTag: 'type:ui', onlyDependOnLibsWithTags: ['type:core'] },
            { sourceTag: 'type:core', onlyDependOnLibsWithTags: [] },
            { sourceTag: 'type:tooling', onlyDependOnLibsWithTags: [] },
          ],
        },
      ],
      '@typescript-eslint/no-explicit-any': 'error',
    },
  },
];
