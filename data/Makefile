# Encoding data conversion and verification
#
# Usage:
#   make          # Convert .tiktoken -> .ttkb, copy to encoding dirs, verify
#   make convert  # Only convert .tiktoken -> .ttkb
#   make copy     # Copy .ttkb files to encoding project directories
#   make verify   # Verify .ttkb files match .tiktoken sources
#   make clean    # Remove generated .ttkb files from this directory

ENCODINGS := o200k_base cl100k_base r50k_base p50k_base
TTKB_FILES := $(addsuffix .ttkb,$(ENCODINGS))
SRC_DIR := ../src/libs

.PHONY: all convert copy verify clean

all: convert copy verify

convert: $(TTKB_FILES)

%.ttkb: %.tiktoken
	python3 convert_to_ttkb.py $<

copy: $(TTKB_FILES)
	cp o200k_base.ttkb $(SRC_DIR)/Tiktoken.Encodings.o200k/
	cp cl100k_base.ttkb $(SRC_DIR)/Tiktoken.Encodings.cl100k/
	cp r50k_base.ttkb $(SRC_DIR)/Tiktoken.Encodings.r50k/
	cp p50k_base.ttkb $(SRC_DIR)/Tiktoken.Encodings.p50k/
	@echo "Copied .ttkb files to encoding directories."

verify: $(TTKB_FILES)
	python3 verify_ttkb.py

clean:
	rm -f $(TTKB_FILES)
