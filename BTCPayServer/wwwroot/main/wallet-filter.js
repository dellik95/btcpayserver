function initFilters() {
    let options = [];
    let container = null;
    let input = null;
    let addedLabels = [];
    const queryParamName = "labelFilter";


    function init() {
        options = Array.from(document.querySelectorAll("#filter-input-list  > option"));
        container = document.getElementById("filters-container");
        input = document.getElementById("filter-input");
    }

    function subscribe() {
        subscribeForApplyFilter();
        subscribeForDeleteLabel();
    }

    function restoreFilters() {
        let url = new URLSearchParams(window.location.search);
        let params = url.get(queryParamName);
        if (!params) return;

        let paramValues = decodeURI(params).split(",")
        for (const label of paramValues) {
            createLabel(label);
        }
    }
    
    
    function canApplyFilter(value) {
        return value && value.trim().length > 0 && !addedLabels.includes(value);
    }
    
    

    function subscribeForDeleteLabel() {
        let labels = $(".delete-filter-label");
        for (const label of labels) {
            let labelText = label?.parentElement?.getAttribute("data-tLabel") ?? "";
            $(label).on("click", e => {
                e.stopPropagation();
                removeItemAll(addedLabels, labelText);
                buildUrlAndNavigate();
            });
        }
    }


    function subscribeForApplyFilter() {
        $(input).on("keyup", e => {
            if (e.key !== "Enter") {
                return false
            }
            onFilterApplyEvent();
        });

        $("#apply-filter-btn").on("click", onFilterApplyEvent);
    }

    function onFilterApplyEvent() {
        let labelText = input.value.trim();
        if (labelText && canApplyFilter(labelText)) {
            let created = createLabel(labelText);
            if (created && created.length > 0) {
                buildUrlAndNavigate();
            }
        }
        input.value = "";
    }


    function buildUrlAndNavigate() {
        let urlParams = new URLSearchParams({
            [queryParamName]: addedLabels
        });
        const urlPieces = [location.protocol, '//', location.host, location.pathname];

        if (addedLabels.length !== 0) {
            urlPieces.push(`?${urlParams.toString()}`);
        }
        location.href = urlPieces.join('');
    }


    function createLabel(text) {
        let mappedOptions = filterAndMapOptionsByName(text);
        for (const labelEl of mappedOptions) {
            createLabelElement(labelEl.text, labelEl.textColor, labelEl.color);
            addedLabels.push(labelEl.text);
        }
        return mappedOptions;
    }


    function filterAndMapOptionsByName(lText) {
        return options.filter(x => {
            let labelText = x?.getAttribute("data-tLabel-text");
            return labelText === lText;
        }).map(e => {
            return {
                color: e?.getAttribute("data-tLabel-color"),
                textColor: e?.getAttribute("data-tLabel-textColor"),
                text: e?.getAttribute("data-tLabel-text")
            }
        }) ?? [];
    }


    function createLabelElement(labelText, labelTextColor, labelColor) {
        let template = `
            <div data-tLabel="${labelText}" class="badge position-relative text-white flex-grow-0" style="background-color:${labelColor};color:${labelTextColor} !important;">
                <a style="background-color:inherit;color:inherit;">${labelText}</a>
                <span class="fa fa-close delete-filter-label"></span>
            </div> 
        `;
        $(container).append(template);
    }


    function removeItemAll(arr, value) {
        let i = 0;
        while (i < arr.length) {
            if (arr[i] === value) {
                arr.splice(i, 1);
            } else {
                ++i;
            }
        }
        return arr;
    }

    init();
    restoreFilters();
    subscribe();
}
