addInstallButton('reactor', 'ipadapter', 'ipadapter', 'Install IP Adapter (Install first)');

// The ReActor install button logic is a bit more complicated as it must only be installed after IP Adapter
postParamBuildSteps.push(() => {
    let reactorGroup = document.getElementById('input_group_content_reactor');
    if (reactorGroup && !currentBackendFeatureSet.includes('reactor')) {
        // Disabled button by default
        reactorGroup.append(createDiv(`reactor_install_button`, 'keep_group_visible', `<button disabled class="basic-button" onclick="installFeatureById('reactor', 'reactor_install_button')">Install ReActor</button>`));
    }
});
hideParamCallbacks.push(() => {
    let installButton = document.getElementById(`reactor_install_button`);
    if (installButton) {
        let button = installButton.querySelector("button");
        if (button) {
            // Hacky way to check if the feature is currently installing so we don't reenable the button during that process
            let divs = installButton.querySelectorAll("div");
            let reactorInstalling = false;
            divs.forEach(div => {
                if (div.textContent.includes("Installing...")) {
                    reactorInstalling = true;
                }
            });
            // Disables the button until ipadapter is installed
            button.disabled = reactorInstalling || !currentBackendFeatureSet.includes('ipadapter');
        }
        if (currentBackendFeatureSet.includes('reactor')) {
            installButton.remove();
        }
    }
});
