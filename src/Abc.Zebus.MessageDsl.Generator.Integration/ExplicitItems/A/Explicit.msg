
// The files in the "A" and "B" directories have the same name and are added explicitly
// to the project (without using wildcards). This causes %(RecursiveDir) to be empty which leads to a name collision.

ExplicitlyDefinedMessage();
