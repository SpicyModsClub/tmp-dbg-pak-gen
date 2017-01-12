#tmp\_generate\_dbg\_pak

This is a temporary tool used to generate a dbg.pak.xen file for use with
QueenBee to add names to files that would otherwise be checksums.  It is
complete for its specific purpose, but that is all.  It does not allow for
appending to an existing dbg.pak.xen file, for example.

It takes as input a list of checksums and filenames in this format:

1234ABCD *tab* long\\file\\path\\to\\file.ext