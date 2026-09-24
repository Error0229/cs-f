// Several bundled formatters are self-extracting launchers that rewrite the same files under
// %TEMP% on every start (php-cs-fixer, ktlint). Two of them starting at once can collide.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
